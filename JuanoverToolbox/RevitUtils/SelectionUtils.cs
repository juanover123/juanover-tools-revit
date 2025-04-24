using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JuanoverToolbox.RevitUtils
{
    class SelectionUtils
    {
        public static IList<Element> GetManyRefByRectangle(UIDocument uidoc)
        {
            try
            {
                ReferenceArray ra = new ReferenceArray();
                ISelectionFilter selFilter = new WallSelectionFilter();
                IList<Element> eList = uidoc.Selection.PickElementsByRectangle(selFilter, "Seleccionar muros") as IList<Element>;
                return eList;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("¡Atención!", "Debes seleccionar uno o más muros para continuar");
                throw ex;
            }

        }

        public static IList<Element> GetElementsFromReferences(IList<Reference> references, UIDocument uidoc)
        {
            IList<Element> elements = new List<Element>();
            foreach (Reference reference in references)
            {
                Element element = uidoc.Document.GetElement(reference);
                elements.Add(element);
            }
            return elements;

        }

        public class WallSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                if (element.Category != null && element.Category.BuiltInCategory == BuiltInCategory.OST_Walls)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference refer, XYZ point)
            {
                return false;
            }
        }
    }
}
