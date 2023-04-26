using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Larvend
{
    public class Chart
    {
        public string title;
        public string composer;
        public string arranger;
        public float bpm;
        public float offset;
        public int count;

        public enum Difficulties
        {
            Light,
            Order,
            Chaos,
            Error
        }

        public List<Note> notes;

        public Chart()
        {
            this.title = "Sample Song";
            this.composer = "Sample Artist";
            this.arranger = "Sample Arranger";
            this.bpm = 120;
            this.offset = 0;
            this.count = 0;
            this.notes = new List<Note>();
        }
        public Chart(string title, string composer, string arranger, float bpm, float offset, int count, List<Note> notes)
        {
            this.title = title;
            this.composer = composer;
            this.arranger = arranger;
            this.bpm = bpm;
            this.offset = offset;
            this.count = count;
            this.notes = notes;
        }

        public void UpdateChartInfo(string title, string composer, string arranger, float bpm, float offset)
        {
            this.title = title;
            this.composer = composer;
            this.arranger = arranger;
            this.bpm = bpm;
            this.offset = offset;
        }
    }
}
