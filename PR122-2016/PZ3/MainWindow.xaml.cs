using PZ3.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace PZ3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static double minX = 19.793909;
        private static double maxX = 19.894459;
        private static double minY = 45.2325;
        private static double maxY = 45.277031;

        static int xDimension = 100;
        static int yDimension = 100;
        static int zDimension = 100;
        public static List<String> opcije = new List<string>() {"ALL", "1", "2", "3",};
        private Tuple<string, object>[,,] viewportMatrix = new Tuple<string, object>[xDimension, yDimension, zDimension];
        private Dictionary<long, Tuple<string, PowerEntity>> powerEntities = new Dictionary<long, Tuple<string, PowerEntity>>();
        //private Tuple<string, PowerEntity> connectionEntities = new Tuple<string, PowerEntity>();
        private Dictionary<long, LineEntity> linesEntities = new Dictionary<long, LineEntity>();
        private Dictionary<long, GeometryModel3D> nodes3D = new Dictionary<long, GeometryModel3D>();
        private Dictionary<long, GeometryModel3D> nodes3DTemp = new Dictionary<long, GeometryModel3D>();
        private Dictionary<long, GeometryModel3D> lines3D = new Dictionary<long, GeometryModel3D>();
        private List<Tuple<long, SolidColorBrush>> coloredElements = new List<Tuple<long, SolidColorBrush>>();
        public double newX, newY;

        private Point start = new Point();
        private Point diffOffset = new Point();

        private GeometryModel3D hitgeo;
        private ToolTip toolTip = new ToolTip();
        
        private int zoomMax = 40;
        private int unzoomMax = -2;
        private int zoomCurent = 1;
        
        private bool mouseLeftButtonPressed = false;
        private bool mouseMiddleButtonPressed = false;
        //private bool changed = false;
       
        public MainWindow()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");

            //load substation
            XmlNodeList substationList;
            substationList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in substationList)
            {
                SubstationEntity substationEntity = new SubstationEntity();

                substationEntity.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                substationEntity.Name = node.SelectSingleNode("Name").InnerText;
                substationEntity.X = double.Parse(node.SelectSingleNode("X").InnerText);
                substationEntity.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(substationEntity.X, substationEntity.Y, 34, out newY, out newX);
                substationEntity.X = newX;
                substationEntity.Y = newY;

                if (CheckRange(substationEntity.X, substationEntity.Y, maxX, maxY))
                {
                    powerEntities.Add(substationEntity.Id, new Tuple<string, PowerEntity>("substation", substationEntity));
                }
                else { continue; }

            }

            //load node
            XmlNodeList nodeList;
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {
                NodeEntity nodeEntity = new NodeEntity();

                nodeEntity.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nodeEntity.Name = node.SelectSingleNode("Name").InnerText;
                nodeEntity.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nodeEntity.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(nodeEntity.X, nodeEntity.Y, 34, out newY, out newX);
                nodeEntity.X = newX;
                nodeEntity.Y = newY;

                if(CheckRange(nodeEntity.X,nodeEntity.Y,maxX,maxY))
                {
                    powerEntities.Add(nodeEntity.Id, new Tuple<string, PowerEntity>("node", nodeEntity));
                }
                else { continue; }

            }

            //load switch
            XmlNodeList switchList;
            switchList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in switchList)
            {
                SwitchEntity switchEntity = new SwitchEntity();

                switchEntity.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                switchEntity.Name = node.SelectSingleNode("Name").InnerText;
                switchEntity.X = double.Parse(node.SelectSingleNode("X").InnerText);
                switchEntity.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                switchEntity.Status = node.SelectSingleNode("Status").InnerText;

                ToLatLon(switchEntity.X, switchEntity.Y, 34, out newY, out newX);
                switchEntity.X = newX;
                switchEntity.Y = newY;

                if (CheckRange(switchEntity.X,switchEntity.Y, maxX, maxY))
                {
                    powerEntities.Add(switchEntity.Id, new Tuple<string, PowerEntity>("switch", switchEntity));
                }
                else { continue; }
            }
            
            //load line
            XmlNodeList lineList;
            lineList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in lineList)
            {
                LineEntity l = new LineEntity();

                l.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                l.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                {
                    l.IsUnderground = true;
                }
                else
                {
                    l.IsUnderground = false;
                }
                l.R = float.Parse(node.SelectSingleNode("R").InnerText);
                l.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                l.LineType = node.SelectSingleNode("LineType").InnerText;
                l.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                l.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                l.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);

                if (!powerEntities.ContainsKey(l.FirstEnd) || !powerEntities.ContainsKey(l.SecondEnd))
                {
                    //powerEntities[l.FirstEnd].Item2.Connections++;
                   //powerEntities[l.SecondEnd].Item2.Connections++;
                    continue;
                }
                
                bool exists = false;
                foreach (LineEntity line in linesEntities.Values)
                {
                    if ((line.FirstEnd == l.FirstEnd || line.FirstEnd == l.SecondEnd) &&
                        (line.SecondEnd == l.FirstEnd || line.SecondEnd == l.SecondEnd))
                    {
                        exists = true;
                        break;
                    }
                }
                if (powerEntities.ContainsKey(l.FirstEnd) && powerEntities.ContainsKey(l.SecondEnd))
                {
                    powerEntities[l.FirstEnd].Item2.Connections++;
                    powerEntities[l.SecondEnd].Item2.Connections++;
                    
                }
                if (exists)
                {
                    continue;
                }

                l.Vertices = new List<Point>();

                foreach (XmlNode pointNode in node.ChildNodes[9].ChildNodes)
                {
                    Point p = new Point();

                    p.X = double.Parse(pointNode.SelectSingleNode("X").InnerText);
                    p.Y = double.Parse(pointNode.SelectSingleNode("Y").InnerText);

                    ToLatLon(p.X, p.Y, 34, out newY, out newX);
                    if (CheckRange(newX,newY, maxX, maxY))
                    {
                        l.Vertices.Add(new Point(newX, newY));
                    }
                    else { continue; }
                }

                linesEntities.Add(l.Id, l);
            }

           
            AddEntities();
        }

        private void AddEntities()
        {
            foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
            {
                AddPowerEntity(powerEntity);
                
            }
            foreach (LineEntity lineEntity in linesEntities.Values)
            {
                AddLineEntity(lineEntity);
            }
        }     
        private void AddPowerEntity(Tuple<string, PowerEntity> powerEntity)
        {
            double multiply = 0.02;
            double addsub = 0.005;
            
            Point3D newPoint = new Point3D();
            newPoint.X = Math.Round(Scale(powerEntity.Item2.X, minX, maxX, 0, xDimension - 1));
            newPoint.Z = zDimension - 1 - Math.Round(Scale(powerEntity.Item2.Y, minY, maxY, 0, zDimension - 1));
            newPoint.Y = 0;

            while (viewportMatrix[(int)newPoint.X, (int)newPoint.Z, (int)newPoint.Y] != null)
            {
                newPoint.Y++;
            }

            viewportMatrix[(int)newPoint.X, (int)newPoint.Z, (int)newPoint.Y] = new Tuple<string, object>(powerEntity.Item1, powerEntity.Item2);


            GeometryModel3D node = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();


            mesh.Positions.Add(new Point3D(newPoint.X * multiply - addsub, (newPoint.Y + 1) * multiply - addsub, newPoint.Z * multiply - addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply + addsub, (newPoint.Y + 1) * multiply - addsub, newPoint.Z * multiply - addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply - addsub, (newPoint.Y + 1) * multiply + addsub, newPoint.Z * multiply - addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply + addsub, (newPoint.Y + 1) * multiply + addsub, newPoint.Z * multiply - addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply - addsub, (newPoint.Y + 1) * multiply - addsub, newPoint.Z * multiply + addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply + addsub, (newPoint.Y + 1) * multiply - addsub, newPoint.Z * multiply + addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply - addsub, (newPoint.Y + 1) * multiply + addsub, newPoint.Z * multiply + addsub));
            mesh.Positions.Add(new Point3D(newPoint.X * multiply + addsub, (newPoint.Y + 1) * multiply + addsub, newPoint.Z * multiply + addsub));

           
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(5);


            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(6);

            
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(2);

           
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(3);

            
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(4);

            node.Geometry = mesh;

            switch (powerEntity.Item1)
            {
                case "node":
                    node.Material = new DiffuseMaterial(Brushes.RoyalBlue);
                    break;
                case "substation":
                    node.Material = new DiffuseMaterial(Brushes.Tomato);
                    break;
                case "switch":
                    node.Material = new DiffuseMaterial(Brushes.BurlyWood);
                    break;
            }

            Map.Children.Add(node);
            nodes3D.Add(powerEntity.Item2.Id, node);


        }
        private void AddLineEntity(LineEntity lineEntity)
        {
            double multiply = 0.02;
            double addsub = 0.0025;

            GeometryModel3D line = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int i = 0; i < lineEntity.Vertices.Count; i++)
            {
                Point3D newPoint = new Point3D();

                newPoint.X = Math.Round(Scale(lineEntity.Vertices[i].X, minX, maxX, 0, xDimension - 1));
                newPoint.Z = zDimension - 1 - Math.Round(Scale(lineEntity.Vertices[i].Y, minY, maxY, 0, zDimension - 1));
                newPoint.Y = 0;

                mesh.Positions.Add(new Point3D(newPoint.X * multiply, (newPoint.Y + 1) * multiply + addsub, newPoint.Z * multiply));
                mesh.Positions.Add(new Point3D(newPoint.X * multiply, (newPoint.Y + 1) * multiply - addsub, newPoint.Z * multiply));
            }

            for (int i = 0; i < mesh.Positions.Count - 2; i++)
            {
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(i + 2);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(i + 1);
                mesh.TriangleIndices.Add(i + 2);
            }

            line.Geometry = mesh;
            line.Material = new DiffuseMaterial(CheckResistance(lineEntity));
            
         
            Map.Children.Add(line);
            lines3D.Add(lineEntity.Id, line);
        }

        //mouse events
        private void Zoom(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(this);
            double scaleX = 1;
            double scaleY = 1;
            double scaleZ = 1;
            if (e.Delta > 0 && zoomCurent < zoomMax)
            {
                scaleX = scale.ScaleX + 0.1;
                scaleY = scale.ScaleY + 0.1;
                scaleZ = scale.ScaleZ + 0.1;
                zoomCurent++;
                scale.ScaleX = scaleX;
                scale.ScaleY = scaleY;
                scale.ScaleZ = scaleZ;
            }
            else if (e.Delta <= 0 && zoomCurent > unzoomMax)
            {
                scaleX = scale.ScaleX - 0.1;
                scaleY = scale.ScaleY - 0.1;
                scaleZ = scale.ScaleZ - 0.1;
                zoomCurent--;
                scale.ScaleX = scaleX;
                scale.ScaleY = scaleY;
                scale.ScaleZ = scaleZ;
            }
        }
        private void viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                mouseLeftButtonPressed = true;
                viewport_MouseLeftButtonDown(sender, e);
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                mouseMiddleButtonPressed = true;
                viewport_MouseMiddleButtonDown(sender, e);
            }
        }
        private void mainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                mouseLeftButtonPressed = false;
            }
            else if (e.MiddleButton == MouseButtonState.Released)
            {
                mouseMiddleButtonPressed = false;
            }
            viewport.ReleaseMouseCapture();
        }
        private void viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            start = e.GetPosition(this);
            diffOffset.X = translate.OffsetX;
            diffOffset.Y = translate.OffsetZ;

            toolTip.IsOpen = false;
            Point mousePosition = e.GetPosition(viewport);
            Point3D Point = new Point3D(mousePosition.X, mousePosition.Y, 0);
            Vector3D Direction = new Vector3D(mousePosition.X, mousePosition.Y, 4);

            PointHitTestParameters pointparams = new PointHitTestParameters(mousePosition);
            RayHitTestParameters rayparams = new RayHitTestParameters(Point, Direction);

            hitgeo = null;
            VisualTreeHelper.HitTest(viewport, null, HTResult, pointparams);
        }
        private void viewport_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            start = e.GetPosition(mainWindow);
            diffOffset.X = translate.OffsetX;
            diffOffset.Y = translate.OffsetZ;
            
        }
        private void RotateMove(object sender, MouseEventArgs e)
        {
            if (viewport.IsMouseCaptured)
            {
                Point end = e.GetPosition(this);
                double offsetX = end.X - start.X;
                double offsetY = end.Y - start.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = (offsetX * 100) / w;
                double translateY = (offsetY * 100) / h;

                if (mouseLeftButtonPressed)
                {
                    translate.OffsetX = diffOffset.X + (translateX / (100 * scale.ScaleX));
                    translate.OffsetZ = diffOffset.Y + (translateY / (100 * scale.ScaleZ));
                }
                else if (mouseMiddleButtonPressed)
                {
                    rotateY.Angle = (rotateY.Angle + translateX) % 360;
                    double rotationXAngle = rotateX.Angle + translateY % 360;
                    if (rotationXAngle > -25 && rotationXAngle < 120)
                    {
                        rotateX.Angle = rotationXAngle;
                    }

                    start = end;
                }
            }
        }
        
        //hit test
        private HitTestResultBehavior HTResult(HitTestResult rawResult)
        {
            RayHitTestResult rayResult = rawResult as RayHitTestResult;

            if (rayResult != null)
            {
                bool hit = false;

                foreach (long key in nodes3D.Keys)
                {
                    if (nodes3D[key] == (GeometryModel3D)rayResult.ModelHit)
                    {
                        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                        Tuple<string, PowerEntity> powerEntity = powerEntities[key];
                        toolTip.Content = "\t\t" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(powerEntity.Item1.ToLower()) + "\nId:\t" + powerEntity.Item2.Id + "\nName:\t" + powerEntity.Item2.Name;
                        if (powerEntity.Item1 == "switch")
                            toolTip.Content += "\nStatus:\t" + ((SwitchEntity)powerEntity.Item2).Status;
                        toolTip.IsOpen = true;
                        toolTip.StaysOpen = true;

                        hit = true;
                    }
                }

                if (!hit)
                {
                    foreach (long key in lines3D.Keys)
                    {
                        if (lines3D[key] == (GeometryModel3D)rayResult.ModelHit)
                        {
                            if (coloredElements.Count != 0)
                            {
                                lines3D[coloredElements[0].Item1].Material = new DiffuseMaterial(coloredElements[0].Item2);
                                nodes3D[coloredElements[1].Item1].Material = new DiffuseMaterial(coloredElements[1].Item2);
                                nodes3D[coloredElements[2].Item1].Material = new DiffuseMaterial(coloredElements[2].Item2);
                            }

                            coloredElements.Clear();

                            hitgeo = (GeometryModel3D)rayResult.ModelHit;
                            LineEntity lineEntity = linesEntities[key];

                            coloredElements.Add(new Tuple<long, SolidColorBrush>(key, CheckResistance(lineEntity)));

                            switch (powerEntities[lineEntity.FirstEnd].Item1)
                            {
                                case "node":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.RoyalBlue));
                                    break;
                                case "substation":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.Tomato));
                                    break;
                                case "switch":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.FirstEnd, Brushes.BurlyWood));
                                    break;
                            }

                            switch (powerEntities[lineEntity.SecondEnd].Item1)
                            {
                                case "node":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.RoyalBlue));
                                    break;
                                case "substation":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.Tomato));
                                    break;
                                case "switch":
                                    coloredElements.Add(new Tuple<long, SolidColorBrush>(lineEntity.SecondEnd, Brushes.BurlyWood));
                                    break;
                            }

                            lines3D[key].Material = new DiffuseMaterial(Brushes.Chocolate);
                            nodes3D[lineEntity.FirstEnd].Material = new DiffuseMaterial(Brushes.Chocolate);
                            nodes3D[lineEntity.SecondEnd].Material = new DiffuseMaterial(Brushes.Chocolate);

                            hit = true;
                        }
                    }
                }

                if (!hit)
                {
                    hitgeo = null;
                    toolTip.IsOpen = false;
                }
            }

            return HitTestResultBehavior.Stop;
        }

        //help methods
        private bool CheckRange(double X ,double Y, double maxX,double maxY){
           
            if(X < minX || X > maxX || Y < minY || Y > maxY)
            {
                return false;
            }
               return true;
        }
        private SolidColorBrush CheckResistance(LineEntity lineEntity)
        {
            if (lineEntity.R < 1)
            {
                return Brushes.Red;
            }
            else if (lineEntity.R >= 1 && lineEntity.R <= 2)
            {
                return Brushes.Orange;
            }
            else
            {
                return Brushes.Yellow;
            }
        }
        /*
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.D1)
            {
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if(powerEntity.Item2.Connections >= 0 && powerEntity.Item2.Connections <= 3)
                    {
                        AddPowerEntity(powerEntity);
                    }
                }
            }
            else if(e.Key == Key.D2)
            {
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if (powerEntity.Item2.Connections >= 3 && powerEntity.Item2.Connections <= 5)
                    {
                        AddPowerEntity(powerEntity);
                    }
                }
            }
            else if(e.Key == Key.D3)
            {
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if (powerEntity.Item2.Connections > 5)
                    {
                        AddPowerEntity(powerEntity);
                    }
                }
            }
            else
            {
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                        AddPowerEntity(powerEntity);
                }
            }

            foreach (LineEntity lineEntity in linesEntities.Values)
            {
                AddLineEntity(lineEntity);
            }
        }    
        */
        private void HideAndSeek(object sender, SelectionChangedEventArgs e)
        {
            
            string text = Options.SelectedItem.ToString();
            if (text.Contains("1"))
            {
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if (powerEntity.Item2.Connections >= 0 && powerEntity.Item2.Connections < 3)
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (!Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Add(nodes3D[Id]);
                                }
                               // nodes3DTemp.Add(Id, nodes3D[Id]);
                            }
                            else
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }
                            }
                        }
                        // AddPowerEntity(powerEntity);
                    }
                    else
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }

                            }
                        }
                    }
                }

            }
            else if (text.Contains("2"))
            {
                // Map.Children.Clear();
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if (powerEntity.Item2.Connections >= 3 && powerEntity.Item2.Connections < 5)
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (!Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Add(nodes3D[Id]);
                                }
                               // nodes3DTemp.Add(Id, nodes3D[Id]);
                            }
                            else
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }

                            }
                        }
                    }
                }
            }
            else if (text.Contains("3"))
            {
                // Map.Children.Clear();
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    if (powerEntity.Item2.Connections >= 5)
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (!Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Add(nodes3D[Id]);
                                }
                               // nodes3DTemp.Add(Id, nodes3D[Id]);
                            }
                            else
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (long Id in nodes3D.Keys)
                        {
                            if (Id == powerEntity.Item2.Id)
                            {
                                if (Map.Children.Contains(nodes3D[Id]))
                                {
                                    Map.Children.Remove(nodes3D[Id]);
                                }

                            }
                        }
                    }
                }
            }
            else
            {
                foreach (GeometryModel3D model in nodes3D.Values)
                {
                    if (Map.Children.Contains(model))
                    {
                        Map.Children.Remove(model);
                    }
                }
                nodes3D.Clear();
                // Map.Children.Clear();
                foreach (Tuple<string, PowerEntity> powerEntity in powerEntities.Values)
                {
                    AddPowerEntity(powerEntity);
                }
            }

            
        }
        
        private double Scale(double value, double min, double max, int minScale, int maxScale)
        {
            return minScale + (double)(value - min) / (max - min) * (maxScale - minScale);
        }

  
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
    }
}
