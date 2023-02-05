using System.Diagnostics;
using VisioAddIn.OwlShapes;

namespace VisioAddIn.Snapping
{
    /// <summary>
    /// A C# representation of an sbd page used for snapping checks inside the model.
    /// Not to be confused with <see cref="VisioSubjectBehavior"/> which is used when importing a model from an owl file
    /// </summary>
    public class SBDPage : DiagramPage
    {
        private SBDPage extends;
        private SBDPage foreground;

        private readonly string modelUri;
        public SBDPage(string layer, string nameU, string modelUri) : base(layer, nameU)
        {
            Debug.Print("Creating SBDPage for: " + nameU);
            this.extends = null;
            this.foreground = null;
            this.modelUri = modelUri;
        }

        public void setForeground(SBDPage newProperty)
        {
            foreground = newProperty;
        }

        public SBDPage getForeground()
        {
            return foreground;
        }

        internal SBDPage getExtends()
        {
            return this.extends;
        }

        public void setExtends(SBDPage newProp)
        {
            this.extends = newProp;
        }
        public string getModelUri()
        {
            return modelUri;
        }
    }
}
