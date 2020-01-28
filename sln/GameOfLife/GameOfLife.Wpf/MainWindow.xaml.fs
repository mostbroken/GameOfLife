namespace GameOfLife.Wpf

open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Controls.Primitives

type MainWindowXaml = FsXaml.XAML<"MainWindow.xaml">

[<Struct>]
type WorldSize ={
    Height:int
    Width:int}

[<Struct>]
type Cell = 
    {   IsAlive:bool
        X:int
        Y:int }

type Population = 
    {   Cells: Cell list 
        Size:WorldSize
        Generation:int}

type MainWindow() as this =
    inherit MainWindowXaml()
    
    let rnd = System.Random()
    let rows = 50
    let columns = 50
    let getRandomBool () = (rnd.Next 100) < 20

    let generateWorld rows columns= 
        let cells = [for r in [0..rows-1] do
                        for c in [0..columns-1] do
                            yield { IsAlive= getRandomBool ()
                                    X = c
                                    Y = r}
                        ]
        { Cells=cells
          Size = { Height = rows
                   Width = columns}
          Generation = 1} 
                   
    let getCell world x y = world.Cells|> List.find (fun c-> c.X = x && c.Y = y)
 
    let getNeighbors world cell =
        let up = if cell.Y = 0 then world.Size.Height - 1 else (cell.Y - 1)
        let down = if cell.Y = world.Size.Height - 1 then 0 else (cell.Y + 1)
        let left = if cell.X = 0 then world.Size.Width - 1 else (cell.X - 1)
        let right = if cell.Y = world.Size.Height - 1 then 0 else (cell.Y + 1)
        let getCellInWorld = getCell world

        [
            yield getCellInWorld left up 
            yield getCellInWorld cell.X up
            yield getCellInWorld right up
            yield getCellInWorld left cell.Y 
            yield getCellInWorld right cell.Y
            yield getCellInWorld left down 
            yield getCellInWorld cell.X down 
            yield getCellInWorld right down 
        ]

    let getAliveNeighborsCount world cell =
        (getNeighbors world cell) |> List.filter (fun c -> c.IsAlive)|> List.length

    let getDeadNeighborsCount world cell =
        (getNeighbors world cell) |> List.filter (fun c -> c.IsAlive = false)|> List.length

    let mutateCell world cell = 
        let aliveCount = getAliveNeighborsCount world cell
        let deadCount = getDeadNeighborsCount world cell

        match (cell.IsAlive, aliveCount, deadCount) with
        | (false, 3 ,_) -> {cell with IsAlive = true}
        | (true, l ,_) when l < 2 || l > 3 -> {cell with IsAlive = false}
        | _ -> cell

    let mutateWorld world = 
        let mutatedCells = world.Cells |> List.map (fun c -> mutateCell world c)
        { world with 
            Cells = mutatedCells
            Generation = world.Generation + 1}
        
    let getCellViews() = [for r in this._worldField.Children do r :?> Rectangle]

    let updateCellView (viewAndCell:(Rectangle*Cell)) =
        let (r,c) = viewAndCell
        r.Fill <- if c.IsAlive then System.Windows.Media.Brushes.Green else  System.Windows.Media.Brushes.White
        ()

    let drawWorld world =
        List.zip (getCellViews()) world.Cells |> List.iter updateCellView

    let initCellViews rows columns = 
        this._worldField.Rows <- rows   
        this._worldField.Columns <- columns

        for _ in [0..rows-1] do
            for _ in [0..columns-1] do
                let cell = Rectangle()
                cell.StrokeThickness <- 0.0
                this._worldField.Children.Add(cell) |> ignore
                ()

    let whenLoaded _ =  
        this.SnapsToDevicePixels <- true
        initCellViews rows columns

        let workflowInSeries = async {
            
                let mutable world = generateWorld rows columns

                while true do
                    this.Dispatcher.Invoke(fun () -> (
                                                        drawWorld world
                                                        this.Title <- sprintf "Game of life. Generation = %A" world.Generation))
                    world <- mutateWorld world
                    do! Async.Sleep 500
            }

        Async.Start workflowInSeries 

        ()

    do
        this.Loaded.Add whenLoaded