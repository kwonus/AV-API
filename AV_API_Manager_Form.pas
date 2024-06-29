unit AV_API_Manager_Form;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.ExtCtrls, Vcl.StdCtrls,
  Vcl.WinXCtrls,

  IdHashMessageDigest, idHash,
  System.IOUtils, ShlObj,
  System.Diagnostics, TlHelp32, IdHTTP;

type
  TForm1 = class(TForm)
    Image1: TImage;
    ToggleSwitch1: TToggleSwitch;
    Label1: TLabel;
    WarningLabel: TLabel;
    VersionLabel: TLabel;
    VersionText: TLabel;
    procedure FormCreate(Sender: TObject);
    procedure OnToggle(Sender: TObject);
  private
    { Private declarations }
    function GetURLAsString(const aURL: string): string;

  public
    { Public declarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.dfm}
const
//ApiHash: string = '46E8FE1EAEEB036568EBA365C0E0FE39';
  ApiName: string = 'AV-API.exe';
  ApiRoot: string = 'http://127.0.0.1:1769/';
  ApiStat: string = 'http://127.0.0.1:1769/status';
  ApiExit: string = 'http://127.0.0.1:1769/exit?id=';

  procedure TForm1.FormCreate(Sender: TObject);
  var
    response: string;
    idx: integer;
    api: string;
    toggleOn: Boolean;
    toggleOff: Boolean;
    isOn: Boolean;
    hwnd: integer;
  begin
    WarningLabel.StyleElements := Label1.StyleElements - [seFont]; // enable color changes on this label;
    hwnd := FindWindow(nil, 'AV-Bible Addin for Microsoft Word - API Manager');

    // This application forces a single instance
    if hwnd <> 0
    then begin
      ShowWindow(hwnd, SW_SHOW);
      SetForegroundWindow(hwnd);
      Application.Terminate();
    end
    else begin
      self.Caption := 'AV-Bible Addin for Microsoft Word - API Manager';
    end;
      response := self.GetURLAsString(ApiRoot);

    idx := response.IndexOf('AV-Engine Version:');
    isOn := (idx >= 0);

    self.ToggleSwitch1.State := TToggleSwitchState.tssOn;
    if not isOn
      then self.OnToggle(nil)
      else begin
        idx := idx + Length('AV-Engine Version:');
        self.VersionText.Caption := response.Substring(idx+1).Trim();
      end;
  end;

 //returns MD5 has for a file
 function MD5(const fileName : string) : string;
 var
   idmd5 : TIdHashMessageDigest5;
   fs : TFileStream;
   hash : T4x4LongWordRecord;
   hashstr : string;
 begin
   idmd5 := TIdHashMessageDigest5.Create;
   fs := TFileStream.Create(fileName, fmOpenRead OR fmShareDenyWrite) ;
   result := '';
   try
     SetLength(hashstr, 32);
     result := idmd5.HashStringAsHex(hashstr);
   finally
     fs.Free;
     idmd5.Free;
   end;
 end;

function GetSpecialFolder(const CSIDL: integer) : string;
var
  RecPath : PWideChar;
begin
  RecPath := StrAlloc(MAX_PATH);
    try
    FillChar(RecPath^, MAX_PATH, 0);
    if SHGetSpecialFolderPath(0, RecPath, CSIDL, false)
      then result := RecPath
      else result := '';
    finally
      StrDispose(RecPath);
    end;
end;

 function VerifyExeHash(): string;
 var
  path: string;
  folder: string;
  apipath: string;
  hash: string;
 begin
  SetLength(path, MAX_PATH);
  SetLength(path, GetModuleFileName(0, PChar(path), MAX_PATH));
  folder := ExtractFilePath(path);
  apipath := TPath.Combine(folder, ApiName);
  hash := MD5(apipath);
  {
  if hash = ApiHash
    then result := apipath
    else result := '';
  }
  result := apipath;
 end;

procedure LaunchExternalProcess(const ExePath: string);
var
  StartupInfo: TStartupInfo;
  ProcessInfo: TProcessInformation;
begin
  FillChar(StartupInfo, SizeOf(StartupInfo), 0);
  StartupInfo.cb := SizeOf(StartupInfo);
//  StartupInfo.dwFlags := STARTF_USESHOWWINDOW;
  StartupInfo.wShowWindow := SW_HIDE;

  if CreateProcess(
    PChar(ExePath),     // Executable path
    nil,                // Command line (use ExePath as-is)
    nil,                // Process handle not inheritable
    nil,                // Thread handle not inheritable
    False,              // Don't inherit handles
    0,                  // No creation flags
    nil,                // Use parent's environment block
    nil,                // Use parent's starting directory
    StartupInfo,        // Startup Info
    ProcessInfo         // Process Info
  ) then
  begin
    // Successfully launched the process
    CloseHandle(ProcessInfo.hProcess);
    CloseHandle(ProcessInfo.hThread);
  end
  else
  begin
    // Failed to launch the process
    ShowMessage('Error launching process: ' + SysErrorMessage(GetLastError));
  end;
end;

function TForm1.GetURLAsString(const aURL: string): string;
var
  lHTTP: TIdHTTP;
begin
  lHTTP := TIdHTTP.Create;
  try
    result := lHTTP.Get(aURL);
  except
    result := '';
  end;
  lHTTP.Free;
end;

procedure TForm1.OnToggle(Sender: TObject);
var
  response: string;
  idx: integer;
  api: string;
  isOn: Boolean;
begin
  response := GetURLAsString(ApiRoot);

  idx := response.IndexOf('AV-Engine Version:');
  isOn := (idx >= 0);

  if (self.ToggleSwitch1.State = TToggleSwitchState.tssOn) and not isOn
  then begin
    api := VerifyExeHash();
    if Length(api) > 0
    then begin
      LaunchExternalProcess(api);
      Sleep(1500);
      self.ToggleSwitch1.State := TToggleSwitchState.tssOn;

      response := GetURLAsString(ApiRoot);

      idx := response.IndexOf('AV-Engine Version:');
      isOn := (idx >= 0);

      if not isOn // it did not launch
        then self.ToggleSwitch1.State := TToggleSwitchState.tssOff
        else begin
          idx := idx + Length('AV-Engine Version:');
          self.VersionText.Caption := response.Substring(idx+1).Trim();
        end;
    end;
  end
  else if (self.ToggleSwitch1.State = TToggleSwitchState.tssOff) and isOn
  then begin
    // Force exit() of API

    response := GetURLAsString(ApiStat);
    response := GetURLAsString(ApiExit+response);
    response := GetURLAsString(ApiRoot);

    idx := response.IndexOf('AV-Engine Version:');
    isOn := (idx >= 0);

    if isOn  // it is still alive
      then self.ToggleSwitch1.State := TToggleSwitchState.tssOn;
  end;
  if isOn
    then self.WarningLabel.Font.Color := clBlack
    else self.WarningLabel.Font.Color := clMaroon;
end;

function ProcessExists(exeFileName: string): Boolean;
var
  ContinueLoop: BOOL;
  FSnapshotHandle: THandle;
  FProcessEntry32: TProcessEntry32;
begin
  FSnapshotHandle := CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
  FProcessEntry32.dwSize := SizeOf(FProcessEntry32);
  ContinueLoop := Process32First(FSnapshotHandle, FProcessEntry32);
  Result := False;
  while Integer(ContinueLoop) <> 0 do
  begin
    if (UpperCase(ExtractFileName(FProcessEntry32.szExeFile)) = UpperCase(exeFileName)) then
    begin
      Result := True;
      Break;
    end;
    ContinueLoop := Process32Next(FSnapshotHandle, FProcessEntry32);
  end;
  CloseHandle(FSnapshotHandle);
end;

end.
