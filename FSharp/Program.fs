module MandelbrotSet =
    open System
    open System.Numerics

    let compute maxIterations number =
        let mandelbrotFormula z c = z * z + c

        let rec mandelbrotRec c z iteration =
            if iteration >= maxIterations then
                iteration
            elif Complex.Abs(z) > 2. then
                iteration + 1 - int(log(Math.Log(2., Complex.Abs(z))))
            else
                mandelbrotRec c (mandelbrotFormula z c) (iteration + 1)

        mandelbrotRec number Complex.Zero 0

module Renderer =
    open FSharp.Collections.ParallelSeq
    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.PixelFormats
    open SixLabors.ImageSharp.ColorSpaces
    open SixLabors.ImageSharp.ColorSpaces.Conversion

    let convertHsvToRgba32 (hsv: Hsv) =
        let converter = ColorSpaceConverter()
        let rgb = converter.ToRgb(&hsv)
        Rgba32(rgb.R, rgb.G, rgb.B)

    let getColor maxIterations m =
        let hue = int (float (255 * m) / (float maxIterations))
        let saturation = 255
        let value = if m < maxIterations then 255 else 0
        
        Hsv(float32 hue, float32 saturation, float32 value)
        |> convertHsvToRgba32

    let drawMandelbrotSet (width, height) maxIterations mandelbrotSet outStream =
        use img = new Image<Rgba32>(width, height)

        mandelbrotSet
        |> PSeq.iter (fun (x, y, iterationCount) ->
            img.[x, y] <- getColor maxIterations iterationCount
        )
        
        img.SaveAsPng(outStream)

open System.IO
open System.Numerics
open FSharp.Collections.ParallelSeq
open CommandLine

type Parameters = {
        [<Option("max-iterations", HelpText = "Number of iterations to compute to check if a number escapes the Mandelbrot set")>] MaxIterations: int option
        [<Option("real-start-bound", HelpText = "Start of the Real numbers")>] RealNumberStartBound: float option
        [<Option("real-end-bound", HelpText = "End of the Real numbers")>] RealNumberEndBound: float option
        [<Option("imaginary-start-bound", HelpText = "Start of the Imaginary numbers")>] ImaginaryNumberStartBound: float option
        [<Option("imaginary-end-bound", HelpText = "End of the Imaginary numbers")>] ImaginaryNumberEndBound: float option
        [<Option('w', "width", HelpText = "Width of the generated image")>] ImageWidth: int option
        [<Option('h', "height", HelpText = "Height of the generated image")>] ImageHeight: int option
        [<Option('o', "output-path", HelpText = "Path of the generated image")>] ImageOutputPath: string option
    }

let tryReadParameters args =
    let parameters = CommandLine.Parser.Default.ParseArguments<Parameters>(args)
    match parameters with
    | :? Parsed<Parameters> as parsedParameters -> Some (parsedParameters.Value)
    | _ -> None

let inline getOrDefault fn defaultValue (opt: Parameters option) =
    opt |> Option.bind fn |> Option.defaultValue defaultValue

let coordinatesToComplex (realBounds, imaginaryBounds) (width, height) (x, y) =
    let convert (startBound, endBound) maxValue v =
        startBound + (float v / float maxValue) * (endBound - startBound)

    let x' = convert realBounds width x
    let y' = convert imaginaryBounds height y
    Complex (x', y')

[<EntryPoint>]
let main argv =
    let parameters = tryReadParameters argv
    let maxIterations = parameters |> getOrDefault (fun p -> p.MaxIterations) 80
    let realStartBound = parameters |> getOrDefault (fun p -> p.RealNumberStartBound) -2.5
    let realEndBound = parameters |> getOrDefault (fun p -> p.RealNumberEndBound) 1.
    let imaginaryStartBound = parameters |> getOrDefault (fun p -> p.ImaginaryNumberStartBound) -1.
    let imaginaryEndBound = parameters |> getOrDefault (fun p -> p.ImaginaryNumberEndBound) 1.
    let imageWidth = parameters |> getOrDefault (fun p -> p.ImageWidth) 600
    let imageHeight = parameters |> getOrDefault (fun p -> p.ImageHeight) 400
    let imageOutputPath = parameters |> getOrDefault (fun p -> p.ImageOutputPath) "mandelbrot.png"

    let realBounds = realStartBound, realEndBound
    let imaginaryBounds = imaginaryStartBound, imaginaryEndBound
    let imageSize = imageWidth, imageHeight

    let coordinatesToComplex = coordinatesToComplex (realBounds, imaginaryBounds) imageSize

    let sw = System.Diagnostics.Stopwatch()
    sw.Start()

    let mandelbrotSet =
        Seq.allPairs
            (Seq.init imageWidth id)
            (Seq.init imageHeight id)
        |> PSeq.map (fun (x, y) ->
            let complexNumber = coordinatesToComplex (x, y)
            let iterationCount = MandelbrotSet.compute maxIterations complexNumber
            (x, y, iterationCount)
        )

    use outStream = File.OpenWrite(imageOutputPath)
    Renderer.drawMandelbrotSet imageSize maxIterations mandelbrotSet outStream

    sw.Stop()
    printfn "Image generated in %ims" sw.ElapsedMilliseconds

    0 // return an integer exit code
