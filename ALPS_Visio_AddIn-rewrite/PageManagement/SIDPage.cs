using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALPS_Visio_AddIn_rewrite
{
    /// <summary>
    /// represents a subject interaction diagram
    /// </summary>
    public class SIDPage : DiagramPage, IComparable<SIDPage>
    {
        private int priorityOrder;
        private string modelUri;

        private SIDPage extends;
        private SIDPage foreground;

        private readonly List<SBDPage> sbdPages;

        public SIDPage(string layer, string nameU, string modelUri, int priorityOrder) : base(layer, nameU)
        {
            Debug.Print("Creating SIDPage for: " + nameU);
            this.priorityOrder = priorityOrder;
            this.modelUri = modelUri;

            this.sbdPages = new List<SBDPage>();
            this.foreground = null;
            this.extends = null;
        }

        /// <summary>
        /// searches after a specified sbd page by nameU.
        /// </summary>
        internal SBDPage getSbdPage(string subjectShapeId)
        {
            subjectShapeId = subjectShapeId.Trim(new Char[] { '\\', '"' });
            return sbdPages.FirstOrDefault(sbdPage => sbdPage.getNameU().Equals(subjectShapeId));
        }

        internal IList<SBDPage> getTreeView()
        {
            return sbdPages.ToList();
        }

        public void setLayer(string newName)
        {
            this.layer = newName;
        }

        internal void setForeground(SIDPage newProperty)
        {
            foreground = newProperty;
        }

        public SIDPage getForeground()
        {
            return this.foreground;
        }

        internal SIDPage getExtends()
        {
            return this.extends;
        }

        internal void setExtends(SIDPage newProp)
        {
            this.extends = newProp;
        }

        public string getModelUri()
        {
            return modelUri;
        }

        public string getModelUriForUser()
        {
            return modelUri.Trim('\\', '"');
        }

        public void setModelUri(string modelUri)
        {
            this.modelUri = modelUri;
        }

        /// <summary>
        /// compareTo Method. Lower numbers have higher priority
        /// </summary>
        public int CompareTo(SIDPage x)
        {
            return x.getPriorityOrder() > this.priorityOrder ? 1 : 0;
        }

        internal int getPriorityOrder()
        {
            return this.priorityOrder;
        }

        internal void setPriorityOrder(int newPriority)
        {
            priorityOrder = newPriority;
        }

        internal IList<SBDPage> getSbdPages()
        {
            return sbdPages;
        }

        internal void addSbdPage(SBDPage sbdPage)
        {
            sbdPages.Add(sbdPage);
        }
    }
}
