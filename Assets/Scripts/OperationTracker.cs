using System;
using System.Collections.Generic;
using Larvend.Gameplay;
using UnityEngine;

namespace Larvend
{
    public enum OperationType
    {
        Create,
        Delete,
        Modify
    }

    public class Operation
    {
        public OperationType Type;
        public Line Target;
        public Line Origin;

        public Operation(OperationType type, Line origin, Line target)
        {
            Type = type;
            Target = target;
            Origin = origin;
        }
        
    }

    public class OperationGroup
    {
        public List<Operation> Operations = new List<Operation>();

        public OperationGroup()
        {
            Operations = new List<Operation>();
        }

        public OperationGroup(Operation operation)
        {
            Operations = new List<Operation>
            {
                operation
            };
        }
    }
    
    public class OperationTracker : MonoBehaviour
    {
        public static OperationTracker Instance { get; set; }
        public List<OperationGroup> OperationGroups = new List<OperationGroup>();

        private void Start()
        {
            Instance = this;
            OperationGroups = new List<OperationGroup>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z) && !Global.IsPlaying)
            {
                Debug.Log("Reveal Invoke");
                Reveal();
            }
        }

        public static void Record(Operation operation)
        {
            if (Instance.OperationGroups.Count >= 10)
            {
                Instance.OperationGroups.RemoveAt(0);
            }
            Instance.OperationGroups.Add(new OperationGroup(operation));
        }

        public static void Record(OperationGroup operationGroup)
        {
            if (Instance.OperationGroups.Count >= 10)
            {
                Instance.OperationGroups.RemoveAt(0);
            }
            Instance.OperationGroups.Add(operationGroup);
        }

        public static void EditTarget(Line line)
        {
            var operationGroup = Instance.OperationGroups[^1];
            var operation = operationGroup.Operations[^1];
            operation.Target = line;
        }

        public void Reveal()
        {
            if (OperationGroups.Count == 0)
            {
                return;
            }

            var operationGroup = OperationGroups[^1];
            foreach (var operation in operationGroup.Operations)
            {
                switch (operation.Type)
                {
                    case OperationType.Create:
                        RevealCreate(operation);
                        break;
                    case OperationType.Delete:
                        RevealDelete(operation);
                        break;
                    case OperationType.Modify:
                        RevealModify(operation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            OperationGroups.RemoveAt(OperationGroups.Count - 1);
        }

        public void RevealCreate(Operation operation)
        {
            var note = NoteManager.Instance.Find(operation.Target);
            if (note)
            {
                note.DeleteSelf();
            }
        }

        public void RevealDelete(Operation operation)
        {
            NoteManager.CreateNote(operation.Origin.type, operation.Origin.time).Copy(operation.Origin);
        }

        public void RevealModify(Operation operation)
        {
            var note = NoteManager.Instance.Find(operation.Target);
            if (note)
            {
                note.UpdateInfo(operation.Origin);
            }
        }

        public static void ClearAll()
        {
            Instance.OperationGroups.Clear();
        }
    }
}