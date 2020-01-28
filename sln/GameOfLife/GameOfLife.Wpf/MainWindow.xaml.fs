namespace GameOfLife.Wpf

open System
open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Controls.Primitives


type MainWindowXaml = FsXaml.XAML<"MainWindow.xaml">

[<Struct>]
type CellState =
    |Alive
    |Dead

[<Struct>]
type Position = {
    X:int
    Y:int}

[<Struct>]
type WorldSize ={
    Height:int
    Width:int}

type Population = 
    {   Cells: CellState array
        WorldSize:WorldSize
        Generation:int64}

type MainWindow() as this =
    inherit MainWindowXaml()
    
    let rnd = System.Random()
    let frameRatePerSecond = 20
    let worldSize = { Height = 80; Width = 80 }
    let populationPercentage = 40
    let getRandomBool percentage = (rnd.Next 100) < percentage
    let mutable started = false

    let generatePopulation ws = 
        let cellsCount = ws.Height * ws.Width
        let generateCellState () = if getRandomBool populationPercentage then CellState.Alive else CellState.Dead
        
        let cells =  {1 .. cellsCount} |> Seq.map (fun _ -> generateCellState()) |> Seq.toArray
        { Cells=cells;WorldSize = ws;Generation = 1L} 
                   
    let getCellState population pos = 
        let shift = pos.Y * population.WorldSize.Width + pos.X
        population.Cells.[shift]

    let getNeighborsStates population cell =
        let up = if cell.Y = 0 then population.WorldSize.Height - 1 else (cell.Y - 1)
        let down = if cell.Y = population.WorldSize.Height - 1 then 0 else (cell.Y + 1)
        let left = if cell.X = 0 then population.WorldSize.Width - 1 else (cell.X - 1)
        let right = if cell.Y = population.WorldSize.Height - 1 then 0 else (cell.Y + 1)

        let neighborPositions = 
            seq{
                yield ( left, up  )                
                yield ( cell.X, up)
                yield ( right, up)
                yield ( left, cell.Y )
                yield ( right, cell.Y)
                yield ( left, down )
                yield ( cell.X, down )
                yield ( right, down )
            }|> Seq.map (fun (x,y) -> {X = x;Y = y})

        neighborPositions 
        |> Seq.map (fun pos -> getCellState population pos)
        |> Seq.toList

    let mutatePopulation population = 
        let getPosFromShift shift =
            let (x,y) = Math.DivRem (shift, population.WorldSize.Width)
            {X = x;Y = y}

        let mutateCell pos cellState = 
            let neighborsStates = getNeighborsStates population pos
            let aliveCount = neighborsStates |> Seq.filter (fun c -> c = CellState.Alive)|> Seq.length
            let deadCount = neighborsStates |> Seq.filter (fun c -> c = CellState.Dead)|> Seq.length

            match (cellState, aliveCount, deadCount) with
            | (CellState.Dead, 3 ,_) -> CellState.Alive
            | (CellState.Alive, ac ,_) when ac < 2 || ac > 3 -> CellState.Dead
            | _ -> cellState


        //population.Cells |> Array.iteri (fun i s -> (
        //                                                let pos = getPosFromShift i
        //                                                let state= mutateCell pos s
        //                                                population.Cells.[i] <- state))

        let mutatedCells = 
            population.Cells 
            |> Seq.mapi (fun i s -> ( let pos = getPosFromShift i
                                      mutateCell pos s))
            
        { population with 
            Cells = mutatedCells |> Seq.toArray
            Generation = population.Generation + 1L}
        
    let getCellViews() = [|for r in this._worldField.Children do r :?> Rectangle|]

    let updateCellView (viewAndCellState:(Rectangle*CellState)) =
        let (v,s) = viewAndCellState
        let brush = 
            match s with
            |CellState.Alive -> System.Windows.Media.Brushes.Green
            |CellState.Dead -> System.Windows.Media.Brushes.White

        v.Fill <- brush
        ()

    let drawWorld population =
        Seq.zip (getCellViews()) population.Cells |> Seq.iter updateCellView

    let initCellViews worldSize = 
        this._worldField.Rows <- worldSize.Height   
        this._worldField.Columns <- worldSize.Width

        for y in [0..worldSize.Height-1] do
            for x in [0..worldSize.Width-1] do
                let cell = Rectangle()
                cell.ToolTip <- sprintf "X=%d;Y=%d" x y
                cell.StrokeThickness <- 0.0
                cell.Fill <- System.Windows.Media.Brushes.Gray
                //cell.StrokeThickness <- 0.5
                //cell.Stroke <- System.Windows.Media.Brushes.Gray

                this._worldField.Children.Add(cell) |> ignore
                ()

    let onLifeCycleBtnClicked _ =
        started <- not started
        this._lifeCycleBtn.Content <- if started then "Stop" else "Start"
        ()

    let onLoaded _ =  
        this.SnapsToDevicePixels <- true
        initCellViews worldSize
        let mutable world = generatePopulation worldSize


        let lifeCycle = async {
            
                let draw _ = 
                    drawWorld world
                    this.Title <- sprintf "Game of life. Generation = %d" world.Generation

                while true do
                    if started then
                        this.Dispatcher.Invoke(draw)
                        world <- mutatePopulation world
                        
                    do! Async.Sleep (1000/frameRatePerSecond)
                }

        Async.Start lifeCycle 
        ()

    do
        this.Loaded.Add onLoaded
        this._lifeCycleBtn.Click.Add onLifeCycleBtnClicked