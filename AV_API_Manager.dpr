program AV_API_Manager;

uses
  Vcl.Forms,
  AV_API_Manager_Form in 'AV_API_Manager_Form.pas' {Form1};

{$R *.res}

begin
  Application.Initialize;
  Application.MainFormOnTaskbar := True;
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
