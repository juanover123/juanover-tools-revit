#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JuanoverToolbox.RevitUtils;
using static JuanoverToolbox.RevitUtils.SelectionUtils;
#endregion

namespace JuanoverToolbox.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class WallExplode : IExternalCommand
    {
        const int ROUNDING_DECIMALS = 3;

        public class WallLayerData
        {
            public Material Material { get; set; }
            public ElementId MaterialId { get; set; }
            public double Thickness { get; set; }
            public MaterialFunctionAssignment Function { get; set; }
            public ElementId CorrespondingWallTypeId { get; set; }
        }

        public (List<WallLayerData>, bool, string) GetWallLayersInfo(Wall wall)
        {
            List<WallLayerData> wallLayerDataList = new List<WallLayerData>();
            string wallInfo = "";

            if (wall != null)
            {
                WallType wallType = wall.Document.GetElement(wall.GetTypeId()) as WallType;

                if (wallType?.GetCompoundStructure() is CompoundStructure wallStructure && wallStructure != null)
                {
                    IList<CompoundStructureLayer> wallLayers = wallStructure.GetLayers();

                    wallInfo += "----- " + wallType.Name + " - Id: " + wallType.Id + " -----\n\n";

                    foreach (CompoundStructureLayer layer in wallLayers)
                    {
                        Element layerMaterialElement = wall.Document.GetElement(layer.MaterialId);
                        Material layerMaterial = layerMaterialElement as Material;
                        double layerWidth = layer.Width;
                        double layerWidthMeters = UnitUtils.ConvertFromInternalUnits(layer.Width, UnitTypeId.Meters);
                        MaterialFunctionAssignment layerFunction = layer.Function;

                        wallInfo += $"Material: {(layerMaterial != null ? layerMaterial.Name : "<Not Found>")}\n";
                        wallInfo += $"Function: {layerFunction}\n";
                        wallInfo += $"Thickness [m]: {Math.Round(layerWidthMeters, ROUNDING_DECIMALS )} m\n\n";

                        wallLayerDataList.Add(new WallLayerData
                        {
                            Material = layerMaterial,
                            MaterialId = layer.MaterialId,
                            Thickness = layerWidthMeters,
                            Function = layerFunction
                        });
                    }

                    wallInfo += "\n";
                }
            }

            return (wallLayerDataList, true, wallInfo);
        }

        public void CreateWallTypesFromLayers(Autodesk.Revit.DB.Document doc
            , IList<WallLayerData> wallLayerDataList
            , IDictionary<string, ElementId> allWallTypesDict
            , WallType originalWallType
            , Material defaultMaterial)
        {
            ElementId defaultMaterialId = defaultMaterial?.Id ?? ElementId.InvalidElementId;

            foreach (WallLayerData wallLayerData in wallLayerDataList)
            {
                Material currentMaterial;
                ElementId currentMaterialId;
                string materialNamePart = "<Error>";

                if (wallLayerData.Material == null || wallLayerData.MaterialId == ElementId.InvalidElementId)
                {
                    if (defaultMaterial != null && defaultMaterialId != ElementId.InvalidElementId)
                    {
                        currentMaterial = defaultMaterial;
                        currentMaterialId = defaultMaterialId;
                        materialNamePart = defaultMaterial.Name;
                        Debug.Print($"Using fallback material '{defaultMaterial.Name}' for layer thickness {wallLayerData.Thickness}m");
                    }
                    else
                    {
                        Debug.Print($"Skipping layer with thickness {wallLayerData.Thickness}m - (No original material and no fallback available).");
                        wallLayerData.CorrespondingWallTypeId = ElementId.InvalidElementId;
                        continue;
                    }
                }
                else
                {
                    if (wallLayerData.Material != null)
                    {
                        currentMaterial = wallLayerData.Material;
                        currentMaterialId = wallLayerData.MaterialId;
                        materialNamePart = currentMaterial.Name;
                    }
                    else
                    {
                        Debug.Print($"Warning: Valid MaterialId {wallLayerData.MaterialId} but Material object is null. Attempting fallback.");
                        if (defaultMaterial != null && defaultMaterialId != ElementId.InvalidElementId)
                        {
                            currentMaterial = defaultMaterial;
                            currentMaterialId = defaultMaterialId; // Usar ID de fallback por seguridad
                            materialNamePart = defaultMaterial.Name;
                        }
                        else
                        {
                            Debug.Print($"Skipping layer... Valid MaterialId {wallLayerData.MaterialId} but object is null and no fallback.");
                            wallLayerData.CorrespondingWallTypeId = ElementId.InvalidElementId;
                            continue;
                        }
                    }
                }

                string formatString = "F" + ROUNDING_DECIMALS;
                string thicknessString = Math.Round(wallLayerData.Thickness, ROUNDING_DECIMALS).ToString(formatString);
                string wallTypeName = $"{materialNamePart}_{thicknessString}m";
                // TODO -> function to clear  / sanitize name

                if (allWallTypesDict.ContainsKey(wallTypeName))
                {
                    ElementId existingWallTypeId = allWallTypesDict[wallTypeName];

                    if (doc.GetElement(existingWallTypeId) is WallType existingWallType)
                    {
                        wallLayerData.CorrespondingWallTypeId = existingWallTypeId;
                        Debug.Print($"Found existing WallType: {existingWallType.Name} ID: {existingWallTypeId}");
                    }

                    else
                    {
                        Debug.Print($"Error: WallType '{wallTypeName}' in dictionary but not found in document (ID: {existingWallTypeId}). Removing from dictionary.");
                        allWallTypesDict.Remove(wallTypeName);
                    }
                }
                if (!allWallTypesDict.ContainsKey(wallTypeName))
                {
                    try
                    {
                        double thicknessInternal = UnitUtils.Convert(wallLayerData.Thickness, UnitTypeId.Meters, UnitTypeId.Feet);
                        if (thicknessInternal <= 0)
                        {
                            Debug.Print($"Skipping layer '{wallTypeName}' due to invalid thickness: {thicknessInternal} feet.");
                            continue;
                        }
                        WallType newWallType = originalWallType.Duplicate(wallTypeName) as WallType;

                        if (newWallType != null)
                        {
                            CompoundStructure newCompoundStructure = CompoundStructure.CreateSingleLayerCompoundStructure(
                                wallLayerData.Function,
                                thicknessInternal,
                                currentMaterialId);

                            if (newCompoundStructure != null)
                            {
                                newWallType.SetCompoundStructure(newCompoundStructure);
                                // newWallType.Width = thicknessInternal; recommended but cant change. read only?

                                allWallTypesDict.Add(wallTypeName, newWallType.Id);
                                wallLayerData.CorrespondingWallTypeId = newWallType.Id;

                                Debug.Print($"Created new WallType: {newWallType.Name} (ID: {newWallType.Id})");
                            }

                            else
                            {
                                Debug.Print($"Error: Failed to create CompoundStructure for WallType '{wallTypeName}'.");
                                try { doc.Delete(newWallType.Id); } catch { /* Ignorar si falla la eliminación */ }
                            }
                        }
                        else
                        {
                            Debug.Print($"Error: Failed to duplicate WallType to create '{wallTypeName}'. Name might be invalid or already in use?");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"Exception creating WallType ´{wallTypeName}´: {ex.Message}");

                    }
                }
            }
        }

        /*public static ICollection<ElementId> CreateLayersFromWall(Autodesk.Revit.DB.Document doc,ElementId wall)
        {
            XYZ vector = new XYZ(10, 10, 0);
            ICollection<ElementId> newElementsId = new List<ElementId>();
            foreach(layer? in wallLayers?)
            {
                ElementTransformUtils.CopyElement(doc, wall, vector);
            }    
            //ICollection<ElementId> newElementsId = ElementTransformUtils.CopyElement(doc, wall, vector);
           
            Element newWall = doc.GetElement(newElementId);
        }*/

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            // 0. Create Default Material
            // TODO -> Future: add dropdown to select other material
            string defaultMaterialName = "0P_AUX_FALLBACK";
            Material defaultMaterial = null;
            ElementId defaultMaterialId = ElementId.InvalidElementId;

            using (Autodesk.Revit.DB.Transaction txMat = new Autodesk.Revit.DB.Transaction(doc))
            {
                txMat.Start("Create Default Material");
                defaultMaterial = new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .FirstOrDefault(m => m.Name.Equals(defaultMaterialName, StringComparison.OrdinalIgnoreCase));

                if (defaultMaterial == null)
                {
                    try
                    {
                        defaultMaterialId = Material.Create(doc, defaultMaterialName);

                        defaultMaterial = doc.GetElement(defaultMaterialId) as Material;

                        if (defaultMaterialId != ElementId.InvalidElementId)
                        {
                            if (defaultMaterial != null)
                            {
                                Debug.Print($"Material '{defaultMaterialName} created succesfully. ID:{defaultMaterialId}");

                                try
                                {
                                    defaultMaterial.Color = new Color(128, 128, 128);
                                    defaultMaterial.Transparency = 0;
                                    defaultMaterial.Shininess = 10;
                                }
                                catch (Exception propEx)
                                {
                                    Debug.Print($"Warning: Material default properties couldn't be assigned to '{defaultMaterialName}': {propEx.Message}");
                                }
                            }
                            else
                            {
                                Debug.Print($"Error: Material.Create returned ID {defaultMaterialId} but GetElement failed");
                                defaultMaterialId = ElementId.InvalidElementId;
                            }
                        }
                        else
                        {
                            Debug.Print($"Error: Material.Create failed for the name '{defaultMaterialName}'.");
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException argEx)
                    {
                        Debug.Print($"Error creating '{defaultMaterialName}': {argEx.Message}. Maybe already exists?");
                        defaultMaterial = new FilteredElementCollector(doc)
                            .OfClass(typeof(Material))
                            .Cast<Material>()
                            .FirstOrDefault(m => m.Name.Equals(defaultMaterialName, StringComparison.OrdinalIgnoreCase));
                        if (defaultMaterial != null) defaultMaterialId = defaultMaterial.Id;
                        else defaultMaterialId = ElementId.InvalidElementId;
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"Unexpected error creating the material '{defaultMaterialName}': '{ex.Message}'");
                        defaultMaterialId = ElementId.InvalidElementId;
                        defaultMaterial = null;
                    }
                }
                else
                {
                    defaultMaterialId = defaultMaterial.Id;
                    Debug.Print($"Material '{defaultMaterialName}' already found. Id: {defaultMaterialId}.");
                }

                txMat.Commit();
            }

            if (defaultMaterial == null || defaultMaterialId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("Critical Error", $"The fallback material: {defaultMaterialName} couldn't be found or created. The operation can't continue.");
                return Result.Failed;
            }

            // 1. Pick Walls
            IList<Element> selectedElements = SelectionUtils.GetManyRefByRectangle(uidoc);

            Dictionary<WallType, List<WallLayerData>> wallDataToWallTypeMapping = new Dictionary<WallType, List<WallLayerData>>();
            List<Wall> walls = new List<Wall>();

            // 2. Read wall materials and layer thickness

            string allWallInfo = "";
            Dictionary<ElementId, List<WallLayerData>> processedLayerDataMap = new Dictionary<ElementId, List<WallLayerData>>();
            List<ElementId> uniqueWallTypeIdsFound = new List<ElementId>();

            try
            {
                if (selectedElements == null || selectedElements.Count == 0)
                {
                    TaskDialog.Show("Alerta", "No hay elementos seleccionados.");
                    return Result.Cancelled;
                }

                List<ElementId> processedWallTypeIds = new List<ElementId>();

                foreach (Element selectedElement in selectedElements)
                {
                    if (selectedElement is Wall wall)
                    {
                        WallType wallType = wall.WallType;

                        (List<WallLayerData> wallLayerData, bool success, string wallInfo) = GetWallLayersInfo(wall);
                        
                        if (success)
                        {
                            if (!wallDataToWallTypeMapping.ContainsKey(wallType))
                            {
                                wallDataToWallTypeMapping.Add(wallType, wallLayerData);
                                walls.Add(wall);

                                if (!processedWallTypeIds.Contains(wallType.Id))
                                {
                                    allWallInfo += wallInfo; 
                                    processedWallTypeIds.Add(wallType.Id);
                                }
                            }
                        }
                        else
                        {
                            TaskDialog.Show("Error", $"Error trying to get information about the layers of the wall with ID: {wall.Id}");
                        }

                    }
                    else
                    {
                        allWallInfo += $"Id: {selectedElement.Id}, Nombre de tipo: {selectedElement.Name} (No es un muro)\n";

                        // Añadir registro detallado para elementos no-muro
                        // TaskDialog.Show("Debug allWallInfo", allWallInfo);
                    }
                }
                TaskDialog.Show("Informacion de los muros y sus capas", allWallInfo);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Ocurrió un error al seleccionar muros: {ex.Message}");
                return Result.Failed;
            }

            // 3. Get all wall types in the project.

            string allWallTypesText = "";
            Dictionary<string, ElementId> allWallTypesDict = new Dictionary<string, ElementId>();

            try
            {
                allWallTypesDict = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .ToDictionary(w => w.Name, w => w.Id);

                foreach (KeyValuePair<string, ElementId> kvp in allWallTypesDict)
                {
                    allWallTypesText += $"Nombre de Tipo: {kvp.Key}, Id: {kvp.Value}\n";
                }
            }
            catch (Exception ex)
            {
                allWallTypesText = $"Un error ocurrió al obtener todos los tipos de muro del proyecto: {ex.Message}";
            }

            TaskDialog.Show("Tipos de muro en el proyecto hasta el momento", allWallTypesText);

            // 4. Create or select existing wall types with correct Material and Thickness

            using (Autodesk.Revit.DB.Transaction tx = new Autodesk.Revit.DB.Transaction(doc))
            {
                tx.Start("Split Wall Layers");
                try
                {
                    foreach (var kvp in wallDataToWallTypeMapping)
                    {
                        CreateWallTypesFromLayers(doc, kvp.Value, allWallTypesDict, kvp.Key, defaultMaterial); // Pass the original WallType
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Wall types couldn't be created:\nError Message:{ex.Message}");
                    return Result.Failed;
                }
                tx.Commit();
            }

            // 5. Create new wall instances and position them correctly

            IList<ElementId> selectedElementsIds = new List<ElementId>();
            foreach (Element element in selectedElements)
            {
                selectedElementsIds.Add(element.Id);
            }

            using (Autodesk.Revit.DB.Transaction txDupWalls = new Autodesk.Revit.DB.Transaction(doc))
            {
                txDupWalls.Start("Duplicate Source Wall");

                foreach(ElementId elementid in selectedElementsIds)
                {
                    //--REMOVE COMMENT--CreateLayersFromWall(doc, elementid);
                }    
                //XYZ translation = new XYZ(10, 0, 0);

                //IList<ElementId> copiedWallsIdsList = copiedWallsIds.ToList();



                txDupWalls.Commit();
            }




            return Result.Succeeded;



            // 5.1 - Copy hosted elements in walls
            // 5.2 - Copy parameters from the source wall
            // 5.3 - Align new walls
            // 5.4 - Lock new walls
            // 5.5 - Join new walls
            // 6. Delete multi-layered Wall


        }
    }
}
