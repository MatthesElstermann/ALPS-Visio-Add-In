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
                ISubject A = r.getCorrespondentA();
                ISubject B = r.getCorrespondentB();

                foreach (IMessageExchange m in ImplementingMessages)
                {

                    ISubject C = m.getSender();
                    ISubject D = m.getReceiver();
                    if ((C == A || C == B) && (D == A || D == B))
                    {
                        Console.WriteLine(r + " violated!");
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

        foreach (Tuple<ICommunicationAct, IImplementingElement<ICommunicationAct>> t in MessageTransitions.Where((a => a.Item1 != null)))
        {
            Console.WriteLine(t.Item1.GetType());
            Console.WriteLine(t.Item2.GetType());

            switch (t.Item1.GetType().ToString())
            {
                case "alps.net.api.StandardPASS.FullySpecifiedSubject":
                    if (t.Item1.GetType() != t.Item2.GetType())
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


}

}
