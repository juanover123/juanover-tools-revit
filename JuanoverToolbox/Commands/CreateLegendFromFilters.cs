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
    public class CreateLegendFromFilters : IExternalCommand
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

            TaskDialog.Show("To be continued", "Work In Progress :)");

            using (Autodesk.Revit.DB.Transaction tx = new Autodesk.Revit.DB.Transaction(doc))
            {
                tx.Start("Transaction Name");
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
