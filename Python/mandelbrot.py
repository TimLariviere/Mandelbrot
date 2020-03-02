from math import log, log2
import time
import multiprocessing
from PIL import Image, ImageDraw
from argparse import ArgumentParser

def mandelbrot(maxIterations, c):
    def mandelbrotFormula(z, c):
        return z * z + c

    z = complex(0, 0)
    iteration = 0

    while abs(z) <= 2 and iteration < maxIterations:
        iteration += 1
        z = mandelbrotFormula(z, c)
    
    if iteration >= maxIterations:
        return iteration

    return iteration + 1 - log(log2(abs(z)))

def render(outputPath, width, height, maxIterations, mandelbrotSet):
    def getColor(maxIterations, iterationCount):
        hue = int(255 * iterationCount / maxIterations)
        saturation = 255
        value = 255 if iterationCount < maxIterations else 0
        return (hue, saturation, value)

    im = Image.new('HSV', (width, height), (0, 0, 0))
    draw = ImageDraw.Draw(im)

    for (x, y, iterationCount) in mandelbrotSet:
        draw.point([x, y], getColor(maxIterations, iterationCount))

    im.convert('RGB').save(outputPath, 'PNG')

def convertPositionToComplexCoordinate(realBounds, imaginaryBounds, size, x, y):
    def convertValueIntoBounds(startBound, endBound, maxValue, value):
        return startBound + (value / maxValue) * (endBound - startBound)

    x2 = convertValueIntoBounds(realBounds[0], realBounds[1], size[0], x)
    y2 = convertValueIntoBounds(imaginaryBounds[0], imaginaryBounds[1], size[1], y)
    return complex(x2, y2)

def computeForCoordinate(tup):
    return (tup[0], tup[1], mandelbrot(args.MaxIterations, tup[2]))

parser = ArgumentParser()
parser.add_argument("-o", "--output-path", dest = "OutputPath", default = 'mandelbrot.png')
parser.add_argument("-w", "--width", dest = "Width", default = 600)
parser.add_argument("-h2", "--height", dest = "Height", default = 400)
parser.add_argument("--max-iterations", dest = "MaxIterations", default = 80)
parser.add_argument("--real-start-bound", dest = "RealStartBound", default = -2.5)
parser.add_argument("--real-end-bound", dest = "RealEndBound", default = 1.0)
parser.add_argument("--imaginary-start-bound", dest = "ImaginaryStartBound", default = -1.0)
parser.add_argument("--imaginary-end-bound", dest = "ImaginaryEndBound", default = 1.0)
args = parser.parse_args()

WIDTH = int(args.Width)
HEIGHT = int(args.Height)

size = WIDTH, HEIGHT
realBounds = float(args.RealStartBound), float(args.RealEndBound)
imaginaryBounds = float(args.ImaginaryStartBound), float(args.ImaginaryEndBound)

start_time = time.time()

mandelbrotSet = [ (x, y, convertPositionToComplexCoordinate(realBounds, imaginaryBounds, size, x, y))
                    for x in range(0, WIDTH)
                    for y in range(0, HEIGHT) ]

pool = multiprocessing.Pool()
mandelbrotSet = pool.map(computeForCoordinate, mandelbrotSet)
pool.close()

render(args.OutputPath, WIDTH, HEIGHT, args.MaxIterations, mandelbrotSet)

print("Image generated in %ims" % ((time.time() - start_time) * 1000))