using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn1.Shapes
{
    class SIDPage : ShapeBase, IShape
    {
        private OntologyResource OwlResource;
        public Visio.Page page = null;
        public string Lable = "";
        public string Extends = "";
        public string Implements = "";
        public string Priority = "";

        public Dictionary<String, IShape> shapes;

        public SIDPage()
        {
            this.shapes = new Dictionary<string, IShape>();
        }

        public void FromOWL(OntologyGraph graph, OntologyResource res)
        {
            this.OwlResource = res;
            Uri PriorityUri = new Uri(OWLGlobalVariables.StandardPassOntNamespace + "hasPriorityNumber");
            foreach (Triple t in res.TriplesWithSubject)
            {
                String ID = this.OWLParseModelComponentID(t);
                if (ID != "")
                {
                    this.modelComponentID = ID;
                }
                String lable = this.OWLParseLable(t);
                if (lable != "")
                {
                    this.Lable = lable;
                }

                if (t.Predicate.NodeType == NodeType.Uri && t.Object.NodeType == NodeType.Literal)
                {
                    Uri pred = ((UriNode)t.Predicate).Uri;
                    if (t.Predicate.NodeType == NodeType.Uri && t.Object.NodeType == NodeType.Literal)
                    {
                        if (pred.AbsoluteUri == PriorityUri.AbsoluteUri)
                        {
                            this.Priority = ((LiteralNode)t.Object).Value.ToString();
                        }
                    }
                }
            }

        }
        public void ConnectOWL(Dictionary<string, IShape> shapes)
        {
            Uri containsUri = new Uri(OWLGlobalVariables.StandardPassOntNamespace + "contains");
            foreach (Triple t in this.OwlResource.TriplesWithSubject)
            {
                if (t.Predicate.NodeType == NodeType.Uri && t.Object.NodeType == NodeType.Uri)
                {
                    Uri predUri = ((UriNode)t.Predicate).Uri;
                    Uri objUri = ((UriNode)t.Object).Uri;
                    string objID = this.OWLGetIDFromUri(this.OwlResource.Graph, objUri);
                    if (predUri.AbsoluteUri == containsUri.AbsoluteUri && objID != "" && shapes.ContainsKey(objID) 
                            && ! this.shapes.ContainsKey(objID) && shapes[objID] != this )
                    {
                        this.shapes.Add(objID, shapes[objID]);
                    }
                }
            }
        }

        public new void ConnectOWLPass2(Dictionary<string, IShape> shapes)
        {
            foreach( IShape shape in shapes.Values )
            {
                if( shape is StandardMessageExchange && this.shapes.ContainsValue(shape) )
                {
                    StandardMessageExchange ex = (StandardMessageExchange)shape;
                    if( ex.Connector != null && ex.Connector.modelComponentID != "" && ! this.shapes.ContainsValue(ex.Connector))
                    {
                        this.shapes.Add(ex.Connector.modelComponentID, ex.Connector);
                    }
                }
            }
        }


        public Visio.Shape PlaceShape(Visio.Page page)
        {
            foreach (IShape shape in this.shapes.Values)
            {
                if( ! (shape is SBDPage))
                {
                    shape.PlaceShape(page);
                }
            }
            return null;
        }
        public Visio.Shape PlaceShape()
        {
            this.PlaceShape(this.page);
            return null;
        }


        public Visio.Page createPage( String modelURI )
        {
            this.page = VisioHelper.CreateSIDPage(this.modelComponentID, this.Lable, modelURI,this.Extends, this.Implements, this.Priority );
            return this.page;
        }
        public void ConnectShape()
        {
        }

        public override String ToString()
        {
            String r = "{";
            r += "\"type\": \"SIDPage\", ";
            r += "\"ID\": \"" + this.modelComponentID + "\", ";
            r += "\"Lable\": \"" + this.Lable.Replace("\n", "").Replace("\r\n", "").Replace("\r", "").Replace("\"", "\\\"") + "\", ";
            r += "\"shapes\": [";
            foreach( IShape s in this.shapes.Values)
            {
                r += s.ToString() + ", ";
            }
            r = r.Substring(0, r.Length-2);
            r += "]";
            r += "}";
            return r;
        }

        public void Layout()
        {
            this.page.Layout();
            this.page.LayoutIncremental((Visio.VisLayoutIncrementalType)((int)Visio.VisLayoutIncrementalType.visLayoutIncrAlign + (int)Visio.VisLayoutIncrementalType.visLayoutIncrSpace),
                Visio.VisLayoutHorzAlignType.visLayoutHorzAlignNone, Visio.VisLayoutVertAlignType.visLayoutVertAlignDefault,
                OWLGlobalVariables.layoutSpacing * 2, OWLGlobalVariables.layoutSpacing, Visio.VisUnitCodes.visMillimeters);
        }

    }

}
