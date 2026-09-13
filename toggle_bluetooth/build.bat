@echo off
:: Sửa lỗi gõ nhầm vvcvarsall.bat thành vcvarsall.bat và cập nhật đường dẫn chuẩn của VS 2025
set "VCVARS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat"

if not exist "%VCVARS_PATH%" (
    :: Nếu đường dẫn trên vẫn không có, tự động chuyển sang cấu trúc thư mục phụ
    set "VCVARS_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat"
)

if not exist "%VCVARS_PATH%" (
    echo Error: Visual Studio vcvarsall.bat not found. Please check your VS installation path.
    exit /b 1
)

echo Setting up Visual Studio Build Environment using: "%VCVARS_PATH%"
call "%VCVARS_PATH%" amd64

echo Compiling toggle_bluetooth.cpp...
:: Thêm cờ /std:c++17 và liên kết runtimeobject.lib để xử lý WinRT
cl.exe /D_SILENCE_EXPERIMENTAL_COROUTINE_DEPRECATION_WARNINGS /EHsc /O2 /std:c++20 /permissive- /D_AMD64_ toggle_bluetooth.cpp /Fe:toggle_bluetooth.exe /link /SUBSYSTEM:CONSOLE runtimeobject.lib Ole32.lib OleAut32.lib Advapi32.lib
if %ERRORLEVEL% neq 0 (
    echo Compilation failed.
    exit /b %ERRORLEVEL%
)

echo Compilation succeeded! Executable created: toggle_bluetooth.exe