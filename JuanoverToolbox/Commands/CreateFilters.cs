#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Transactions;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace JuanoverToolbox.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CreateFilters : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            // En tu formulario o clase principal

            // Lista para almacenar los nombres de los parámetros
            List<string> projectParameterNames = new List<string>();

            // Obtener la definición de los bindings
            DefinitionBindingMapIterator bindingIterator = doc.ParameterBindings.ForwardIterator();

            while (bindingIterator.MoveNext())
            {
                // Obtener la definición del parámetro
                Definition definition = bindingIterator.Key;

                //Agregar a una lista el parametro
                projectParameterNames.Add(definition.Name);

            }

            TaskDialog.Show("To be continued", "Work In Progress :)");
            //projectParametersCbo.ItemsSource = projectParameterNames;

            return Result.Succeeded;
        }
    }
}