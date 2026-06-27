; CC-Desktop-TODO 安装向导 (NSIS + MUI2)
; 用 makensis.exe 编译: makensis setup.nsi

Unicode true

!include "MUI2.nsh"

!define APP_NAME "CC-Desktop-TODO"
!define APP_EXE  "DesktopTodo.exe"
!define APP_VER  "1.0.0"
!define PUBLISHER "Xuechao Zou"

Name "桌面 TODO"
OutFile "..\dist\CC-Desktop-TODO-Setup-${APP_VER}.exe"
InstallDir "$LOCALAPPDATA\Programs\${APP_NAME}"
InstallDirRegKey HKCU "Software\${APP_NAME}" "InstallDir"
RequestExecutionLevel user   ; 安装到用户目录，无需管理员
SetCompressor /SOLID lzma

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; 完成页可选直接运行
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "立即运行 桌面 TODO"

; 向导页面
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "SimpChinese"

Section "安装"
  SetOutPath "$INSTDIR"
  File "..\build\${APP_EXE}"

  ; 写入安装信息
  WriteRegStr HKCU "Software\${APP_NAME}" "InstallDir" "$INSTDIR"

  ; 快捷方式
  CreateShortcut "$DESKTOP\桌面TODO.lnk" "$INSTDIR\${APP_EXE}"
  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\桌面TODO.lnk" "$INSTDIR\${APP_EXE}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\卸载.lnk" "$INSTDIR\Uninstall.exe"

  ; 卸载程序 + 控制面板"添加/删除程序"条目
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "桌面 TODO"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayVersion" "${APP_VER}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "Publisher" "${PUBLISHER}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "InstallLocation" "$INSTDIR"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  ; 关闭可能运行的实例
  ExecWait 'taskkill /IM "${APP_EXE}" /F'

  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  Delete "$DESKTOP\桌面TODO.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\桌面TODO.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\卸载.lnk"
  RMDir "$SMPROGRAMS\${APP_NAME}"

  ; 清理自启注册项
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${APP_NAME}"

  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
  DeleteRegKey HKCU "Software\${APP_NAME}"
SectionEnd
