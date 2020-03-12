import Rhino.Geometry as rg
import ghpythonlib.components as ghc
import random
import math

def area(box):
    boxEval = ghc.DeconstructBox(box)
    sideA = abs(boxEval[1][0]-boxEval[1][1])
    sideB = abs(boxEval[2][0]-boxEval[2][1])
    return [sideA*sideB, sideA, sideB]

def perlin_noise(args):
    x, y = args
    X = int(x) & 255                  
    Y = int(y) & 255                  
    x -= int(x)                                
    y -= int(y)                               
    u = fade(x)                                
    v = fade(y)                                
    A = p[X  ]+Y       
    B = p[X+1]+Y      
    
    return lerp(v, lerp(u, grad(p[A], x, y), grad(p[B], x-1, y)), lerp(u, grad(p[A+1], x, y-1), grad(p[B+1], x-1, y-1)))
 
def fade(t): 
    return t ** 3 * (t * (t * 6 - 15) + 10)
 
def lerp(t, a, b):
    return a + t * (b - a)
 
def grad(hash, x, y):
    h = hash & 15 
    u = x if h<8 else y                
    v = y if h<4 else (x if h in (12, 14) else 0)
    return (u if (h&1) == 0 else -u) + (v if (h&2) == 0 else -v)
 
p = [None] * 512

if not randomEachTime:
    permutation = [151,160,137,91,90,15,
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
       138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180]
else:
    permutation = range(0,256)
    random.shuffle(permutation)

#double permutation to avoid overflow
for i in range(256):
    p[256+i] = p[i] = permutation[i]

crvMinHeight = min([outline.Point(indx).Z for indx in range(0, outline.PointCount)])

planarized = rg.PolylineCurve(rg.Point3d(outline.Point(indx).X, outline.Point(indx).Y, crvMinHeight) for indx in range(0, outline.PointCount))

boxes = [ghc.BoundingBox(planarized, ghc.RotatePlane(rg.Plane.WorldXY, math.radians(angle)))[0] for angle in range(0, 360)]

boundingBox = sorted(boxes, key=area)[0]

nsquaresA = math.ceil(area(boundingBox)[1]/density)
nsquaresB = math.ceil(area(boundingBox)[2]/density)

valLen = int((nsquaresA + 1)*(nsquaresB + 1))

pointGrid = ghc.DivideSurface(boundingBox, nsquaresA, nsquaresB)[0]

zvals = [0] * valLen

for octave in range(1, octaves+1):
    for yy in range(1, int(nsquaresB)+2):
        for xx in range(1, int(nsquaresA)+2):
            noise = perlin_noise([(xx/(speed/(2*octave))), (yy/(speed/(2*octave)))])
            indx = int(((yy-1)*(nsquaresA+1))+xx-1)
            zvals[indx] += noise*(amplitude/(2*octave))

pointsMoved = [rg.Point3d(pnt.X, pnt.Y, pnt.Z+noise) for pnt, noise in zip(pointGrid, zvals)]

bottom = rg.PolylineCurve(rg.Point3d(outline.Point(indx).X, outline.Point(indx).Y, crvMinHeight-minThickness-abs(min(zvals))) for indx in range(0, outline.PointCount))

bottomSurface = ghc.BoundarySurfaces(bottom)
bottomExtruded = ghc.Extrude(bottomSurface, rg.Vector3d(0, 0, minThickness+(abs(max(zvals))+abs(min(zvals)))*2)) #*2 just to be safe

top = ghc.SurfaceFromPoints(pointsMoved, nsquaresA+1, True)

_terrain = ghc.SolidIntersection(top, bottomExtruded)
terrain = ghc.Mirror(_terrain, rg.Plane.WorldYZ)[0] #to mimic AC and RVT orientation
topography = rg.Brep.CreateContourCurves(terrain, rg.Point3d(0, 0, crvMinHeight-abs(min(zvals))), rg.Point3d(0, 0, crvMinHeight+abs(max(zvals))), topographyInterval)