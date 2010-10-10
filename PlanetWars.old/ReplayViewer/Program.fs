#light


open System
open System.Windows.Forms
open System.Drawing

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

let mainForm = new Form(Size = Size(500, 500), Text = "PlanetWars Replay Viewer")

let tabControl = new TabControl(Dock = DockStyle.Fill, Multiline = true)
mainForm.Controls.Add tabControl

let tabControls = 
  [ Map.getTabPage (); 
    PlanetChart.getTabPage ();  
    StackedPlanetChart.getTabPage (); 
    Victories.getTabPage (); 
    VictoriesNo1v1.getTabPage (); 
    VictoriesAndEncriclements.getTabPage ();
    Captures.getTabPage ()]
    
tabControls |> List.iter tabControl.TabPages.Add 


[<STAThread>]
Application.Run mainForm