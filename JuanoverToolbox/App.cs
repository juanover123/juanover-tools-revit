#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Transactions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JuanoverToolbox.RevitUtils;
using static JuanoverToolbox.RevitUtils.SelectionUtils;
#endregion

namespace JuanoverToolbox
{
  class App : IExternalApplication
  {
        string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public Result OnStartup(UIControlledApplication a)
        {
            // Automate compilation
            // if exist "$(AppData)\Autodesk\REVIT\Addins\2025" copy "$(ProjectDir)*.addin" "$(AppData)\Autodesk\REVIT\Addins\2025"
            // if exist "$(AppData)\Autodesk\REVIT\Addins\2025" copy "$(ProjectDir)$(OutputPath)*.dll" "$(AppData)\Autodesk\REVIT\Addins\2025"

            string tabName = "Juanover Tools";
            string starterPanelName = "Juanover Tools";

            try
            {
                //a.CreateRibbonTab(tabName);
            }
            catch (Exception) { /* La pestaña ya existe */ }

            // ---IMAGES --- large image 24px, small one 16px

            /* No usar. Es solo para rutas locales. En este caso el plugin busca en una ruta del equipo J:// etc etc. 
             * que puedo no existir en otra pc
            BitmapImage dino1_24px = new BitmapImage(new Uri("J:\\01-Proyectos\\REVAPI\\JuanoverToolbox\\JuanoverToolbox\\images\\icons8-dinosaurio-24.png")); */

            string strDino1_16px = "pack://application:,,,/JuanoverToolbox;component/images/icons8-dinosaurio-16.png";
            BitmapImage imgDino1_16px = new BitmapImage(new Uri(strDino1_16px, UriKind.Absolute));

            RibbonPanel starterPanel = a.CreateRibbonPanel("Juanover Tools");
            // si le agrego de argumento el tabName CreateRibbonPanel(tabName, starterPanelName) se agrega a un tab personalizado;

            PushButtonData btdExplodeWalls = new PushButtonData("Explotar muros", "Explotar Muros", assemblyName, "JuanoverToolbox.Commands.WallExplode");
            btdExplodeWalls.ToolTip = "Explota un muro en sus correspondientes capas, cada una como un muro único, vinculado, bloqueado y unido con el resto";
            btdExplodeWalls.LongDescription = "Saludo a todos los que tienen acceso a este Add-In. \n" +
                "Puedes saludarme también. Buscame como /arqjuangarcia/ en LinkedIn";
            btdExplodeWalls.LargeImage = imgDino1_16px;


            PushButtonData btdCreateFilters = new PushButtonData("Crear filtros", "Crear filtros", assemblyName, "JuanoverToolbox.Commands.CreateFilters");
            btdCreateFilters.ToolTip = "Crea filtros a partir de los valores existentes colocados en determinado parámetro";
            btdCreateFilters.LongDescription = "Crea filtros a partir de los valores existentes colocados en determinado parámetro";
            btdCreateFilters.LargeImage = imgDino1_16px;

            PushButtonData btdCreateLegendFromFilters = new PushButtonData("Crear leyenda a partir de filtros", "Crear leyenda a partir de filtros", assemblyName, "JuanoverToolbox.Commands.CreateLegendFromFilters");
            btdCreateLegendFromFilters.ToolTip = "Crear leyenda a partir de los filtros aplicados en una vista o plantilla de vista";
            btdCreateLegendFromFilters.LongDescription = "Crear leyenda a partir de los filtros aplicados en una vista o plantilla de vista";
            btdCreateLegendFromFilters.LargeImage = imgDino1_16px;

            IList<RibbonItem> createdItems = starterPanel.AddStackedItems(btdExplodeWalls,btdCreateFilters,btdCreateLegendFromFilters);
            /* This is for when i want a button only (not stacked)
             * PushButton btnExplodeWalls = starterPanel.AddItem(btdExplodeWalls) as PushButton; */

            return Result.Succeeded;
        }

        public Result OnShutdown( UIControlledApplication a )
    {
      return Result.Succeeded;
    }
  }
}
