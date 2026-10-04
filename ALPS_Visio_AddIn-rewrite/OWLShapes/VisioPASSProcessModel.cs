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
            var layerPages = new Dictionary<string, Visio.Page>();
            // Tatsaechlicher pageLayer je Layer-ID: bei einem Re-Import ins selbe Dokument
            // vergibt CreateSIDPage eindeutige Namen ("<id>_2"), die extends-Verweise muessen
            // dann auf genau diese Seiten zeigen statt auf die des ersten Imports.
            var layerNames = new Dictionary<string, string>();

            // Alle Layer-Seiten eines Modells tragen dieselbe Modell-URI. Frueher bekam jede Seite
            // die URI ihres Layers ("base#layerId"): Layer Explorer und VisioPassModelBuilder sahen
            // dann je Layer ein eigenes Modell, Verifikation/BPMN werteten nur einen Layer aus.
            // alps.net.api uebernimmt den Namensraum der Datei nicht: getBaseURI() liefert nur den
            // Platzhalter "baseuri:", der fuer jedes importierte Modell gleich waere. Die URI der
            // Modell-Komponente ("baseuri:<ModellID>") unterscheidet Modelle dagegen.
            string modelUri = this.getUriModelComponentID();

            // First pass: one SID page per layer. The pageLayer cell gets the layer's model ID
            // (a stable, unique name) instead of the former " " placeholder — a blank pageLayer is
            // invalid and stopped SBD pages from registering. Then draw the layer onto its page.
            foreach (IModelLayer modelLayer in layers)
            {
                string layerId = modelLayer.getModelComponentID();
                // Sichtbarer Name "SID: <Ebenen-Label>"; die Ebenen-ID bleibt der interne pageLayer.
                // (Ebenen, die schon "SID…" heissen — z. B. "SID_1" vom Scaffolder — kein doppeltes Praefix.)
                string layerName = VH.DisplayName(modelLayer);
                string pageName = layerName.StartsWith("SID", System.StringComparison.OrdinalIgnoreCase) ? layerName : "SID: " + layerName;
                Visio.Page sidPage = VH.CreateSIDPage(pageName, layerId, modelUri, "", "", "1");
                layerPages[layerId] = sidPage;
                layerNames[layerId] = sidPage.PageSheet.CellsU["Prop." + Constants.Properties.PageLayer].ResultStr[""];

                if (modelLayer is IVisioImportable importable) importable.ImportToVisio(sidPage);
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
                    string extendedId = extendedLayer.getModelComponentID();
                    string extendedName = layerNames.TryGetValue(extendedId, out string uniqueName) ? uniqueName : extendedId;
                    VH.SetProp(foregroundPage.PageSheet, Constants.Properties.Transition.Extends, extendedName);
                }
                catch (System.Exception e)
                {
                    Debug.WriteLine($"[Import] layer-extends wiring for '{modelLayer.getModelComponentID()}' failed: {e.Message}");
                }
            }
        }
        
        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioPASSProcessModel();
        }
    }
}
