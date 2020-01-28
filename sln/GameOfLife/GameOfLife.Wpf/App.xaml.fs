namespace GameOfLife.Wpf

open System

type App = FsXaml.XAML<"App.xaml">

module Main =
    [<STAThread; EntryPoint>]
    let main _ =
        let app = App()
        let mainWindow = new MainWindow()
        app.Run(mainWindow) // Returns application's exit code.