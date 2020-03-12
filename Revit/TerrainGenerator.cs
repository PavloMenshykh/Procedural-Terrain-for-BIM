using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Events;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

namespace TerrainGenerator
{
    //class to store variables as globals 
    public class GlobVars
    {
        //Default number of iterations
        public static int nOctaves = 7;
        public static double amplitude = 15000;
        public static double speed = 50;
        public static int randomvals = 0;
        public static double density = 1500;

        //Hash lookup table as defined by Ken Perlin
        //This is a randomly arranged array of all numbers from 0-255 inclusive

        public static int[] permutation = {151,160,137,91,90,15,
       131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
       190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
       88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
       77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
       102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
       135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
       5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
       223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
       129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
       251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
       49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
       138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180};
}

    //class with UI generations, which calls the main treegeneration class
    public class TerrainPanel : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {

            //Addin tab data
            string RIBBON_PANEL = "Generate procedural terrain";

            //Add a tab
            RibbonPanel panel = app.CreateRibbonPanel(RIBBON_PANEL);

            string assemblyName = Assembly.GetExecutingAssembly().Location;

            //add button for command trigger
            PushButtonData buttonTerrain = new PushButtonData(
                "Generate procedural terrain", "Procedural terrain", assemblyName,
                "TerrainGenerator.TerrainGenerator");

            PushButton pushButton1 = panel.AddItem(buttonTerrain) as PushButton;
            pushButton1.ToolTip = "Click on a closed line to generate terrain";
            pushButton1.Image = BmpImageSource("TerrainGenerator.Resources.TerrainGeneratorSmall.bmp");
            pushButton1.LargeImage = BmpImageSource("TerrainGenerator.Resources.TerrainGenerator.bmp");


            //add text input
            TextBoxData itemOctaves = new TextBoxData("Octaves");
            itemOctaves.Name = "Iterations of noise";

            //add text input2
            TextBoxData itemAmplitude = new TextBoxData("Amplitude");
            itemAmplitude.Name = "Details amplitude";

            //add text input3
            TextBoxData itemFrequency = new TextBoxData("Speed");
            itemFrequency.Name = "Details speed";

            //add text input4
            TextBoxData itemDensity = new TextBoxData("Density");
            itemFrequency.Name = "Mesh grid density";

            //add text input5
            TextBoxData itemThickness = new TextBoxData("MinThickness");
            itemFrequency.Name = "Minimum height of mesh";

            //add text input6
            TextBoxData itemRandom = new TextBoxData("Predefined or not");
            itemFrequency.Name = "Use predefined permutation table or generate";

            IList<RibbonItem> stackedItems = panel.AddStackedItems(itemOctaves, itemAmplitude, itemFrequency);
            if (stackedItems.Count > 1)
            {
                TextBox item1 = stackedItems[0] as TextBox;
                item1.Value = GlobVars.nOctaves;
                item1.ToolTip = "Number of iterations (try not use more than 7)";
                item1.EnterPressed += Refresh;

                //refreshes value picked from textbox on enter press
                void Refresh(object sender, TextBoxEnterPressedEventArgs args)
                {
                    try
                    {
                        TextBox textBoxRefresher = sender as TextBox;
                        GlobVars.nOctaves = Convert.ToInt32(item1.Value.ToString());
                    }
                    catch
                    {
                    }
                }

                TextBox item2 = stackedItems[1] as TextBox;
                item2.Value = GlobVars.amplitude;
                item2.ToolTip = "Noise height";
                item2.EnterPressed += Refresh2;

                //refreshes value picked from textbox on enter press
                void Refresh2(object sender, TextBoxEnterPressedEventArgs args)
                {
                    try
                    {
                        TextBox textBoxRefresher = sender as TextBox;
                        GlobVars.amplitude = Convert.ToDouble(item2.Value.ToString());
                    }
                    catch
                    {
                    }
                }

                TextBox item3 = stackedItems[2] as TextBox;
                item3.Value = GlobVars.speed;
                item3.ToolTip = "Noise density";
                item3.EnterPressed += Refresh3;

                //refreshes value picked from textbox on enter press
                void Refresh3(object sender, TextBoxEnterPressedEventArgs args)
                {
                    try
                    {
                        TextBox textBoxRefresher = sender as TextBox;
                        GlobVars.speed = Convert.ToDouble(item3.Value.ToString());
                    }
                    catch
                    {
                    }
                }
            }

            IList<RibbonItem> stackedItems2 = panel.AddStackedItems(itemDensity, itemRandom);
            if (stackedItems2.Count > 1)
            {
                TextBox item4 = stackedItems2[0] as TextBox;
                item4.Value = GlobVars.density;
                item4.ToolTip = "Mesh grid density";
                item4.EnterPressed += Refresh3;

                //refreshes value picked from textbox on enter press
                void Refresh3(object sender, TextBoxEnterPressedEventArgs args)
                {
                    try
                    {
                        TextBox textBoxRefresher = sender as TextBox;
                        GlobVars.density = Convert.ToDouble(item4.Value.ToString());
                    }
                    catch
                    {
                    }
                }

                TextBox item6 = stackedItems2[1] as TextBox;
                item6.Value = GlobVars.randomvals;
                item6.ToolTip = "0 for Perlins permutation table, 1 for random";
                item6.EnterPressed += Refresh5;

                //refreshes value picked from textbox on enter press
                void Refresh5(object sender, TextBoxEnterPressedEventArgs args)
                {
                    try
                    {
                        TextBox textBoxRefresher = sender as TextBox;
                        GlobVars.randomvals = Convert.ToInt32(item6.Value.ToString());
                    }
                    catch
                    {
                    }
                }
            }


            return Result.Succeeded;
        }


        //to embed image into dll
        public System.Windows.Media.ImageSource BmpImageSource(string embeddedPath)
        {
            Stream stream = this.GetType().Assembly.GetManifestResourceStream(embeddedPath);
            var decoder = new BmpBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return decoder.Frames[0];
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }
    }


    //class for regular tree generation
    [TransactionAttribute(TransactionMode.Manual)]
    public class TerrainGenerator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get UIDocument
            UIDocument uidoc = commandData.Application.ActiveUIDocument;

            //Get Document
            Document doc = uidoc.Document;

            //Use active view for generation
            View view = doc.ActiveView;

            try
            {
                //Pick referenceline
                Reference refTopo = uidoc.Selection.PickObject(ObjectType.Element);

                //Retrieve Element
                ElementId topoId = refTopo.ElementId;
                Element topo = doc.GetElement(topoId);
                TopographySurface surface = topo as TopographySurface;

                BoundingBoxXYZ box = surface.get_BoundingBox(view);

                XYZ max = box.Max;
                XYZ min = box.Min;

                double maxx = max.X;
                double maxy = max.Y;
                double minx = min.X;
                double miny = min.Y;

                XYZ topleft = new XYZ(minx, maxy, 0);
                XYZ bottomright = new XYZ(maxx, miny, 0);

                double domainx = max.DistanceTo(topleft);       //Math.Abs(maxx - minx);
                double domainy = min.DistanceTo(topleft);       //Math.Abs(maxy - miny);

                int nsquaresA = Convert.ToInt32(Math.Ceiling(domainx*10 / (GlobVars.density/30)));
                int nsquaresB = Convert.ToInt32(Math.Ceiling(domainy*10 / (GlobVars.density/30)));

                double distA = domainx / nsquaresA;
                double distB = domainy / nsquaresB;

                int[] permutations;

                //Generate noise
                //double permutation to avoid overflow
                if (GlobVars.randomvals == 0)
                {
                    permutations = GlobVars.permutation;
                }
                else
                {
                    var rand = new Random();
                    IEnumerable<int> numbers = Enumerable.Range(0, 256);
                    permutations = numbers.OrderBy(x => rand.Next()).ToArray();
                }


                int[] p = new int[512];

                for (int x = 0; x < 256; x++)
                {
                    p[x] = permutations[x];
                    p[256 + x] = permutations[x];
                }

                //List<double> zvals = new List<double>();
                double[] zvals = new double[(nsquaresA + 1)*(nsquaresB + 1)];
                List<XYZ> topography = new List<XYZ>();

                for (double octave = 1.0; octave < (GlobVars.nOctaves + 1); octave ++)
                {
                    for (double yy = 1.0; yy<(nsquaresB+2); yy ++)
                    {
                        for (double xx = 1.0; xx<(nsquaresA+2); xx ++)
                        {
                            double noise = perlin_noise((xx / (GlobVars.speed / (2.0 * octave))), (yy / (GlobVars.speed / (2.0 * octave))), p);
                            int indx = (((int)yy - 1) * (nsquaresA+1) + (int)xx-1);

                            zvals[indx] += (noise * ((GlobVars.amplitude / 330.0) / (2.0 * octave)));

                            if ((int)octave == GlobVars.nOctaves)
                            {
                                double xval = minx + (distA * ((int)xx - 1));
                                double yval = miny + (distB * ((int)yy - 1));

                                XYZ point = new XYZ(xval, yval, zvals[indx]);

                                topography.Add(point);
                            }
                        }
                    }
                }

                IList<XYZ> toDelete = surface.GetPoints();

                //Place generated lines in Revit
                using (Transaction trans = new Transaction(doc, "Place Tree"))
                {

                    trans.Start();

                    Plane plane = Plane.CreateByNormalAndOrigin(min.CrossProduct(max), max);
                    SketchPlane sketchPlane = SketchPlane.Create(doc, plane);

                    TopographySurface toposurface = TopographySurface.Create(doc, topography);
                    ICollection<ElementId> deletedIdSet = doc.Delete(topoId);

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }


        public double perlin_noise(double x, double y, int[] p)
        {
            int X = (int)x & 255;
            int Y = (int)y & 255;
            x -= (int)x;
            y -= (int)y;
            double u = fade(x);
            double v = fade(y);
            int A = p[X    ] + Y;
            int B = p[X + 1] + Y;

            return lerp(v, lerp(u, grad(p[A], x, y), grad(p[B], x - 1, y)), lerp(u, grad(p[A + 1], x, y - 1), grad(p[B + 1], x - 1, y - 1)));
        }

        public double fade(double t)
        {
            return t*t*t * (t * (t * 6 - 15) + 10);
        }

        public double lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }

        public double grad(int hash, double x, double y)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14) ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
