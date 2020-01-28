namespace GameOfLife.Wpf

open System
open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Controls.Primitives


type MainWindowXaml = FsXaml.XAML<"MainWindow.xaml">

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
    let worldSize = { Height = 100; Width = 100 }
    let populationPercentage = 40
    let getRandomBool percentage = (rnd.Next 100) < percentage
    let mutable started = false

    let generatePopulation ws = 
        let cellsCount = ws.Height * ws.Width
        let generateCellState () = if getRandomBool populationPercentage then CellState.Alive else CellState.Dead
        let cells = [|0 .. cellsCount-1|] |> Array.map (fun _ -> generateCellState())
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
            [
                yield ( left, up  )                
                yield ( cell.X, up)
                yield ( right, up)
                yield ( left, cell.Y )
                yield ( right, cell.Y)
                yield ( left, down )
                yield ( cell.X, down )
                yield ( right, down )
            ]|> List.map (fun (x,y) -> {X = x;Y = y})

        neighborPositions |> List.map (fun pos -> getCellState population pos)

    let mutatePopulation population = 
        let getPosFromShift shift =
            let (x,y) = Math.DivRem (shift, population.WorldSize.Width)
            {X = x;Y = y}

        let mutateCell pos = 
            let neighborsStates = getNeighborsStates population pos
            let aliveCount = neighborsStates |> List.filter (fun c -> c = CellState.Alive)|> List.length
            let deadCount = neighborsStates |> List.filter (fun c -> c = CellState.Dead)|> List.length
            let cellState = getCellState population pos

            match (cellState, aliveCount, deadCount) with
            | (CellState.Dead, 3 ,_) -> CellState.Alive
            | (CellState.Alive, ac ,_) when ac < 2 || ac > 3 -> CellState.Dead
            | _ -> cellState


        //population.Cells |> Array.iteri (fun i _ -> (
        //                                                let pos = getPosFromShift i
        //                                                let state= mutateCell pos
        //                                                population.Cells.[i] <- state))

        let mutatedCells = 
            population.Cells 
            |> Array.toSeq 
            |> Seq.mapi (fun i _ -> getPosFromShift i)
            |> Seq.map (fun pos -> mutateCell pos)


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

    let drawWorld world =
        Seq.zip (getCellViews()) world.Cells |> Seq.iter updateCellView

    let initCellViews worldSize = 
        this._worldField.Rows <- worldSize.Height   
        this._worldField.Columns <- worldSize.Width

        for _ in [0..worldSize.Height-1] do
            for _ in [0..worldSize.Width-1] do
                let cell = Rectangle()
                cell.StrokeThickness <- 0.0
                this._worldField.Children.Add(cell) |> ignore
                ()

    let onLifeCycleBtnClicked _ =
        started <- not started
        this._lifeCycleBtn.Content <- if started then "Stop" else "Start"
        ()

    let onLoaded _ =  
        this.SnapsToDevicePixels <- true
        initCellViews worldSize


        let lifeCycle = async {
                let mutable world = generatePopulation worldSize
            
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