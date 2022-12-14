using System;
using System.Collections.Generic;
using System.Linq;

namespace VisioAddIn.Snapping
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

        /// <summary>
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="nameU"></param>
        /// <param name="modelUri"></param>
        /// <param name="priorityOrder"></param>
        public SIDPage(string layer, string nameU, string modelUri, int priorityOrder) : base(layer, nameU)
        {
            this.priorityOrder = priorityOrder;
            this.modelUri = modelUri;

            this.sbdPages = new List<SBDPage>();
            this.foreground = null;
            this.extends = null;
        }

        /// <summary>
        /// searches after a specified sbd page by nameU.
        /// </summary>
        /// <param name="subjectShapeId">nameU of searched sbdPage</param>
        /// <returns>the sbd page if found, null otherwise</returns>
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
        /// <param name="x">page that should be compared</param>
        /// <returns>1 if x' priority is higher, 0 otherwise</returns>
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
