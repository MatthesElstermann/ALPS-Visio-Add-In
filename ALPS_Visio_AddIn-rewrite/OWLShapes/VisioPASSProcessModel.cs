using alps.net.api.ALPS;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VH = ALPS_Visio_AddIn_rewrite.VisioHelper;
using Visio = Microsoft.Office.Interop.Visio;

namespace ALPS_Visio_AddIn_rewrite.OWLShapes
{
    public class VisioPASSProcessModel : PASSProcessModel, IVisioImportable
    {
        public VisioPASSProcessModel(string baseURI, string labelForID = null, ISet<IMessageExchange> messageExchanges = null, ISet<ISubject> relationsToModelComponent = null, ISet<ISubject> startSubject = null, string comment = null, string additionalLabel = null, IList<IIncompleteTriple> additionalAttribute = null) : base(baseURI, labelForID, messageExchanges, relationsToModelComponent, startSubject, comment, additionalLabel, additionalAttribute) { }
        protected VisioPASSProcessModel() { }

        public void ImportToVisio(Visio.Page page)
        {
            var layers = this.getAllElements().Values.OfType<IModelLayer>().ToList();
            OWLImporter.LogStep($"Model.ImportToVisio: {layers.Count} Layer gefunden.");
            var layerPages = new Dictionary<string, Visio.Page>();

            // First pass: one SID page per layer. The pageLayer cell gets the layer's model ID
            // (a stable, unique name) instead of the former " " placeholder — a blank pageLayer is
            // invalid and stopped SBD pages from registering. Then draw the layer onto its page.
            foreach (IModelLayer modelLayer in layers)
            {
                string layerId = modelLayer.getModelComponentID();
                OWLImporter.LogStep($"Layer '{layerId}': CreateSIDPage ...");
                Visio.Page sidPage = VH.CreateSIDPage(layerId, layerId, modelLayer.getUriModelComponentID(), "", "", "1");
                OWLImporter.LogStep($"Layer '{layerId}': SID-Seite erstellt.");
                layerPages[layerId] = sidPage;

                if (modelLayer is IVisioImportable importable)
                {
                    OWLImporter.LogStep($"Layer '{layerId}': ImportToVisio ...");
                    importable.ImportToVisio(sidPage);
                    OWLImporter.LogStep($"Layer '{layerId}': ImportToVisio fertig.");
                }
            }

            // Second pass: wire the layer-extends relationship between the SID pages, so an
            // extension / guard / macro layer sits on top of the base layer it extends. Setting the
            // foreground page's extends cell lets the model controller establish a live extends
            // relationship — which is what makes the GBD snap work after import (no SID snap needed).
            foreach (IModelLayer modelLayer in layers)
            {
                if (!modelLayer.isExtension()) continue;
                IModelLayer extendedLayer = modelLayer.getExtendedElement();
                if (extendedLayer == null) continue;
                if (!layerPages.TryGetValue(modelLayer.getModelComponentID(), out Visio.Page foregroundPage)) continue;
                // Guarded: establishing the layer-extends relationship pulls in the whole extends
                // machinery (background page, rectangle, snapping); a failure here must not abort the
                // import or leave the document in a broken state.
                try
                {
                    OWLImporter.LogStep($"Layer-extends '{modelLayer.getModelComponentID()}' -> '{extendedLayer.getModelComponentID()}' ...");
                    VH.SetProp(foregroundPage.PageSheet, Constants.Properties.Transition.Extends, extendedLayer.getModelComponentID());
                    OWLImporter.LogStep($"Layer-extends '{modelLayer.getModelComponentID()}' fertig.");
                }
                catch (System.Exception e)
                {
                    OWLImporter.LogStep($"Layer-extends FEHLER '{modelLayer.getModelComponentID()}': {e.Message}");
                    Debug.WriteLine($"[Import] layer-extends wiring for '{modelLayer.getModelComponentID()}' failed: {e.Message}");
                }
            }
            OWLImporter.LogStep("Model.ImportToVisio: fertig.");
        }
        
        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioPASSProcessModel();
        }
    }
}
