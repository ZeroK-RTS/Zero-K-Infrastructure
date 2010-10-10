#light


open System
open System.Windows.Forms
open System.Drawing
open System.Drawing.Drawing2D
open System.Drawing.Imaging
open System.IO
open Data

let getTabPage () = 
    let tabPage = new TabPage(Text = "Replays")

    let image = new Bitmap(320, 240)

    let toolStripContainer = new ToolStripContainer(Dock = DockStyle.Fill)
    tabPage.Controls.Add toolStripContainer
    let toolStrip = new ToolStrip(GripStyle = ToolStripGripStyle.Hidden, Stretch = true)
    let statusStrip = new StatusStrip()
    tabPage.Controls.Add statusStrip
    toolStripContainer.TopToolStripPanel.Join toolStrip

    let makeButton label = 
        let button = new ToolStripButton(Text = label, DisplayStyle = ToolStripItemDisplayStyle.Text)
        toolStrip.Items.Add button |> ignore
        button

    let previousButton = makeButton "Back"
    let nextButton = makeButton "Forward"
    let playButton = makeButton "Play"
    let resetButton = makeButton "Reset"
    let exportButton = makeButton "Export frames"

    let info = new ToolStripLabel()
    statusStrip.Items.Add info |> ignore
    let legend = new ToolStripLabel("(Planets / Victories / Encirclements)")
    statusStrip.Items.Add legend |> ignore

    let pictureBox = new PictureBox(Dock = DockStyle.Fill, Image = image, SizeMode = PictureBoxSizeMode.Zoom, AutoSize = true)
    toolStripContainer.ContentPanel.Controls.Add pictureBox


    let armLogo = Image.FromFile (if File.Exists "arm.png" then "arm.png" else @"..\..\..\WebSite\factions\arm.png")
    let coreLogo = Image.FromFile (if File.Exists "core.png" then "arm.png" else @"..\..\..\WebSite\factions\core.png")
    let iconSize = Size(16, 16)

    let currentFrame = ref 0

    let frames = turns |> List.filter (fun turn -> turn.HasMapChanged)

    let topBarHeight = 10

    let drawGalaxy (graphics: Graphics) =
        
        graphics.FillRectangle (Brushes.White, graphics.ClipBounds)
        let frame = frames.[!currentFrame]
        let corePlanets, armPlanets = frame.Planets |> Map.to_list |> List.partition (snd >> (=) Core)
        let width = pictureBox.Width
        let toWidth planets =
            Seq.length planets * width / Data.galaxy.Planets.Count 
        graphics.FillRectangle (Brushes.Blue, 0, 0, toWidth armPlanets, topBarHeight)
        graphics.FillRectangle (Brushes.Red, width - (toWidth corePlanets), 0, width, topBarHeight)
        graphics.DrawLine (Pens.Black, width/2, 0, width/2, 10)
        
        graphics.SmoothingMode <- SmoothingMode.HighQuality
        let toControlCoords (pos: PointF) =
            Point(int (pos.X * single pictureBox.Width), int (pos.Y * single pictureBox.Height))
        for link in Data.galaxy.Links do
            let points = link.PlanetIDs |> Array.map (fun id -> toControlCoords (Data.galaxy.GetPlanet id).Position )
            graphics.DrawLine (Pens.Green, points.[0], points.[1])
        let getPlanetRectangle (pos: PointF) sizeMult = 
            let pos = toControlCoords pos
            let width = int (single iconSize.Width * single sizeMult)
            let height = int (single iconSize.Height * single sizeMult)
            Rectangle(pos.X - width / 2, pos.Y - height / 2, width, height)
        for planet in Data.galaxy.Planets do
            match Map.tryfind planet.ID frame.Planets with
            | None  -> graphics.FillEllipse (Brushes.LightGreen, getPlanetRectangle planet.Position 0.5f)
            | Some ownership -> 
                let logo = function
                    | Arm -> armLogo
                    | Core -> coreLogo
                graphics.DrawImage (logo ownership, getPlanetRectangle planet.Position 1.f)
        info.Text <- sprintf "Arm: %d/%d/%d Core: %d/%d/%d"
            (Seq.length armPlanets)
            frame.Victories.[Arm] 
            frame.Captures.[Arm] 
            (Seq.length corePlanets)
            frame.Victories.[Core] 
            frame.Captures.[Core]

    pictureBox.Paint.Add <| fun e -> drawGalaxy e.Graphics
           
    let goForward _ =
        if !currentFrame < frames.Length - 1 then
            incr currentFrame
            pictureBox.Invalidate ()
            
    nextButton.Click.Add goForward

    previousButton.Click.Add <| fun _ ->
        if !currentFrame > 0 then
            decr currentFrame
            pictureBox.Invalidate ()

    exportButton.Click.Add <| fun _ ->
        Directory.CreateDirectory "gifs" |> ignore
        Directory.CreateDirectory "pngs" |> ignore
        let oldFrame = !currentFrame
        let saveFrame i frame = 
            currentFrame := i
            use image = new Bitmap(pictureBox.Width, pictureBox.Height)
            use g = Graphics.FromImage image
            drawGalaxy g
            image.Save (sprintf "gifs\\%05d.gif" i, ImageFormat.Gif)
            image.Save (sprintf "pngs\\%05d.png" i, ImageFormat.Png)
        frames |> List.iteri saveFrame
        currentFrame := oldFrame
        pictureBox.Invalidate ()

           
    let timer = new Timer(Interval = 10)
    timer.Tick.Add goForward

    playButton.Click.Add <| fun _ -> 
        timer.Enabled <- not timer.Enabled
        playButton.Text <- if timer.Enabled then "Stop" else "Play"
        
    resetButton.Click.Add <| fun _ ->
        currentFrame := 0
        timer.Enabled <- false
        playButton.Text <- "Play"
        pictureBox.Invalidate ()
    tabPage