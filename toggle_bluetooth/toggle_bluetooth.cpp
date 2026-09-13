#include <windows.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Devices.Radios.h>
#include <iostream>

using namespace winrt;
using namespace Windows::Devices::Radios;
using namespace Windows::Foundation;

// Khởi tạo môi trường WinRT cho ứng dụng Console
int main() {
    init_apartment();

    try {
        // Lấy danh sách tất cả các bộ thu phát vô tuyến trên PC (Wifi, Bluetooth, Cellular...)
        auto radiosList = Radio::GetRadiosAsync().get();

        bool foundBluetooth = false;

        for (auto&& radio : radiosList) {
            // Kiểm tra nếu thiết bị vô tuyến thuộc loại Bluetooth
            if (radio.Kind() == RadioKind::Bluetooth) {
                foundBluetooth = true;
                RadioState currentState = radio.State();

                // Đảo ngược trạng thái: Nếu đang bật thì tắt, đang tắt thì bật
                RadioState newState = (currentState == RadioState::On) ? RadioState::Off : RadioState::On;
                
                if (newState == RadioState::On) {
                    std::cout << "Switching Bluetooth ON..." << std::endl;
                } else {
                    std::cout << "Switching Bluetooth OFF..." << std::endl;
                }

                // Áp dụng trạng thái mới thay đổi
                radio.SetStateAsync(newState).get();
                std::cout << "Success!" << std::endl;
                break; 
            }
        }

        if (!foundBluetooth) {
            std::cout << "No Bluetooth radio found on this PC." << std::endl;
            return 1;
        }
    }
    catch (hresult_error const& ex) {
        std::wcerr << L"Error: " << ex.message().c_str() << std::endl;
        return 1;
    }

    return 0;
}