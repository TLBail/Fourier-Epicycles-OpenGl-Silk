using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Silk.NET.Maths;
using Svg;

namespace SilkCircle;

public static class SvgParser
{
    public static XElement[] parseSvgToGetPaths(string path) {
        XDocument doc = XDocument.Load(path);
        return doc.Descendants().Where(elem => elem.Name.LocalName.Equals("path")).ToArray();
    }

    public struct PathElement
    {
        public bool isLine { get; set; }
        public float x { get; set; }
        public float y { get; set; }

        public PathElement(bool isLine, float x, float y) {
            this.isLine = isLine;
            this.x = x;
            this.y = y;
        }
    }

    public static List<Vector2D<float>> parsesXelementPath(XElement pathXElement) {
         string pathString = pathXElement.Attribute("d").Value;
         pathString = pathString.ToUpper();
         List<PathElement> pathElements = parsePathString(pathString);
         List<Vector2D<float>> interpolatedPath = new List<Vector2D<float>>();
         for (int i = 0; i < pathElements.Count - 1; i++)
         {
             PathElement startPoint = pathElements[i];
             PathElement endPoint = pathElements[i + 1];

             interpolatedPath.Add(new Vector2D<float>(startPoint.x, startPoint.y));
             if(!pathElements[i].isLine) continue;
             for (float t = 0.2f; t < 1; t += 0.2f)
             {
                 float x = startPoint.x + (endPoint.x - startPoint.x) * t;
                 float y = startPoint.y + (endPoint.y - startPoint.y) * t;
                 interpolatedPath.Add(new Vector2D<float>(x, y));
             }
         }
         interpolatedPath.Add(new Vector2D<float>(pathElements.Last().x, pathElements.Last().y));
         return interpolatedPath;
         
    }

    public static List<PathElement> parsePathString(string pathString) {
        var pointRegex = new Regex(@"([ML])\s*(-?[\d.]+)[\s,]+(-?[\d.]+)", RegexOptions.IgnoreCase);
        var matches = pointRegex.Matches(pathString);

        List<PathElement> points = new List<PathElement>();

        foreach (Match match in matches)
        {
            string command = match.Groups[1].Value.ToUpper();
            float x = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            float y = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            points.Add(new PathElement(command == "M" || command == "L", x, y));
        }

        return points;
    }
    
    public static List<Vector2D<float>> ParseFile(string svgFilePath)
    {
        var doc = XDocument.Load(svgFilePath);
        var paths = doc.Descendants().Where(e => e.Name.LocalName == "path").ToList();
        var dataPoints = new List<Vector2D<float>>();

        for (int i = 0; i < paths.Count; ++i)
        {
            var pathData = paths[i].Attribute("d").Value;
            var svgPath = SvgPathBuilder.Parse(pathData);


            for (int c = 0; c < svgPath.Count; c++)
            {
                var point = svgPath[c];
                dataPoints.Add(new Vector2D<float>(point.End.X, point.End.Y));
            }

        }
        return dataPoints;

    }
    
       
        

}