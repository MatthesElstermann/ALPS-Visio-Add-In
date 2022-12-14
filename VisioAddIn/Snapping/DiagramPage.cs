using System;

namespace VisioAddIn.Snapping
{
    /// <summary>
    /// An abstract representation of any page type that can occur in a PASS model, meaning SBD and SID pages.
    /// </summary>
    public abstract class DiagramPage
    {
        protected string layer;
        protected string nameU;

        protected DiagramPage(string layer, string nameU)
        {
            this.layer = layer;
            this.nameU = nameU;
        }

        /// <summary>
        /// Returns the NameU of the page which is read from the Visio PageSheet
        /// </summary>
        /// <returns></returns>
        internal string getNameU()
        {
            return this.nameU;
        }

        internal string getLayer()
        {
            return this.layer;
        }

        internal string getLayerForUser()
        {
            return layer.Trim('\\', '"');
        }
    }
}
