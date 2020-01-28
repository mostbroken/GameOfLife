namespace GameOfLife.Wpf

open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Controls.Primitives

type MainWindowXaml = FsXaml.XAML<"MainWindow.xaml">

type WorldSize ={
    Height:int
    Width:int}

type Cell = 
    {   IsAlive:bool
        X:int
        Y:int }

type World = 
    {   Cells: Cell list 
        Size:WorldSize}

type MainWindow() as this =
    inherit MainWindowXaml()
    let rnd = System.Random()
    let rows = 150
    let columns = 150

    let getRandomBool () = (rnd.Next 100) < 1

    let generateWorld rows columns= 
        let cells = [for r in [0..rows-1] do
                        for c in [0..columns-1] do
                            yield { IsAlive= getRandomBool ()
                                    X = c
                                    Y = r}
                        ]
        { Cells=cells
          Size = { Height = rows
                   Width = columns}} 
                   
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

    let mutateWorld world = 
        world.Cells
        

    let getCellViews() = [for r in this._grid.Children do r :?> Rectangle]

    let syncViewsAndCells (viewAndCell:(Rectangle*Cell)) =
        let (r,c) = viewAndCell
        r.Fill <- if c.IsAlive then System.Windows.Media.Brushes.White else  System.Windows.Media.Brushes.Black
        ()

    let drawWorld world =
        List.zip (getCellViews()) world.Cells |> List.iter syncViewsAndCells

    let whenLoaded _ =  
        this.SnapsToDevicePixels <- true
        this._grid.Rows <- rows   
        this._grid.Columns <- columns

        for r in [0..rows-1] do
            for c in [0..columns-1] do
                let cell = Rectangle()
                cell.StrokeThickness <- 0.0
                cell.Fill <- System.Windows.Media.Brushes.Black
                cell.Tag <- sprintf "%A-%A" r c
                this._grid.Children.Add(cell) |> ignore
                ()

        let world = generateWorld rows columns
        drawWorld world
        ()



    let whenClosing _ =
        ()

    let whenClosed _ =
        ()

    let btnTestClick _ =
        this.Title <- "Yup, it works!"

    do
        this.Loaded.Add whenLoaded
        this.Closing.Add whenClosing
        this.Closed.Add whenClosed
        //this.btnTest.Click.Add btnTestClick