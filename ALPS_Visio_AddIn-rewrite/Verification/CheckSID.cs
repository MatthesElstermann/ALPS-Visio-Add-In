namespace ALPS_Visio_AddIn_rewrite.Verification
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using alps.net.api.StandardPASS;
using alps.net.api.parsing;
using VDS.RDF;
using alps.net.api.ALPS;



    public class CheckSID
    {
        //Use the communications with the getcorrespondents method to check whether communication exists while being forbidden
        //Klären: Bidirektional oder nicht?
        public int CheckCommunicationRestrictions (IList<ICommunicationRestriction> SpecifiedRestrictions, IList<IMessageExchange> ImplementingMessages )
        {
            int result = 0;
            Console.WriteLine("\nCheck Communication Restrictions:");
            foreach (ICommunicationRestriction r in SpecifiedRestrictions)
            {
                // Spezifikation und Implementierung sind getrennt geparste Modelle — ihre
                // Subjekt-Objekte sind nie identisch. Verglichen wird daher ueber die IDs:
                // ein Implementierungs-Subjekt "ist" ein Spezifikations-Subjekt, wenn es
                // dieselbe ID traegt oder per implements-Verweis darauf zeigt.
                string a = r.getCorrespondentA()?.getUriModelComponentID();
                string b = r.getCorrespondentB()?.getUriModelComponentID();
                if (a == null || b == null) continue;

                foreach (IMessageExchange m in ImplementingMessages)
                {
                    ISet<string> sender = SpecIdsOf(m.getSender());
                    ISet<string> receiver = SpecIdsOf(m.getReceiver());
                    if ((sender.Contains(a) && receiver.Contains(b)) || (sender.Contains(b) && receiver.Contains(a)))
                    {
                        Console.WriteLine(r.getUriModelComponentID() + " violated by " + m.getUriModelComponentID() + "!");
                        result++;
                    }
                }
            }
            if (result == 0)
            {
                Console.WriteLine("SID Restriction Implementation valid.");
                return 1;
            }
            else
            {
                Console.WriteLine("SID Restriction Implementation not valid.");
                return 0;
            }
        }
        /// <summary>
        /// IDs der Spezifikations-Subjekte, die ein Implementierungs-Subjekt vertritt:
        /// die eigene ID plus alle implements-Verweise.
        /// </summary>
        private static ISet<string> SpecIdsOf(ISubject subject)
        {
            var ids = new HashSet<string>();
            if (subject == null) return ids;
            ids.Add(subject.getUriModelComponentID());
            var refs = subject.getImplementedInterfacesIDReferences();
            if (refs != null)
                foreach (string id in refs)
                    if (id != null) ids.Add(id);
            return ids;
        }

    //This method checks whether the implementation references still fulfil the restrictions given by the specification 
    //-- example: a fully specified subect must be implemented as a fully specified subject and cannot be an abstract subject
    public bool CheckSubject(IList<Tuple<ISubject,ISubject>> Subjects)
    {
        Console.WriteLine("\nCheck SID Subject Implementation:");
        bool result = true;
        int FullySpecified = 0;

        foreach (Tuple<ISubject, ISubject> t in Subjects.Where((a =>a.Item1!=a.Item2)))
        {
            Console.WriteLine(t.Item1.GetType());
            Console.WriteLine(t.Item2.GetType());

            switch (t.Item1.GetType().ToString())
            {
                case "alps.net.api.StandardPASS.FullySpecifiedSubject":
                    if (t.Item1.GetType()!=t.Item2.GetType())
                    {
                        Console.WriteLine("Implementation not correct!");
                        FullySpecified++;
                    }
                    break;

               //insert other subject forms here
            }
        

        }
        if (FullySpecified > 0)
        {
            result = false;
        }
        return result;
    }

    public bool CheckMessageconnectors(IList<Tuple<ICommunicationAct, IImplementingElement<ICommunicationAct>>> MessageTransitions)
    {
        //Console.WriteLine("\nCheck SID Transition Implementation:");
        bool result = true;
        int FullySpecified = 0;

        // Item2 == null steht fuer "nicht implementiert" (siehe GetMessageTransitions) —
        // ohne den Filter wirft t.Item2.GetType() darunter eine NullReferenceException.
        foreach (Tuple<ICommunicationAct, IImplementingElement<ICommunicationAct>> t in MessageTransitions.Where((a => a.Item1 != null && a.Item2 != null)))
        {
            Console.WriteLine(t.Item1.GetType());
            Console.WriteLine(t.Item2.GetType());

            // Kommunikationsakte sind Send- bzw. Receive-Funktionen. Frueher wurde hier gegen den
            // Typnamen "FullySpecifiedSubject" verglichen — das traf nie zu, die Pruefung war
            // wirkungslos. Jetzt: eine Send-Funktion der Spezifikation muss als Send-Funktion
            // implementiert sein, eine Receive-Funktion als Receive-Funktion.
            bool specIsSend = t.Item1 is ISendFunction;
            bool specIsReceive = t.Item1 is IReceiveFunction;
            bool implIsSend = t.Item2 is ISendFunction;
            bool implIsReceive = t.Item2 is IReceiveFunction;
            if ((specIsSend && !implIsSend) || (specIsReceive && !implIsReceive))
            {
                Console.WriteLine("Implementation not correct: " + t.Item1.getModelComponentID()
                    + " (" + (specIsSend ? "Send" : "Receive") + ") implemented by "
                    + (t.Item2 as IPASSProcessModelElement)?.getModelComponentID() + " (" + t.Item2.GetType().Name + ")");
                FullySpecified++;
            }
        }
        if (FullySpecified > 0)
        {
            result = false;
        }
        return result;
    }


}

}
