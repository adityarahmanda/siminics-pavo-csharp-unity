#include "pavo_csharp_api.h"

#include "pavo_driver.h"

#include <cstring>
#include <exception>
#include <limits>
#include <memory>
#include <mutex>
#include <string>
#include <vector>

struct PavoHandle
{
    PavoHandle()
        : driver(new pavo::pavo_driver()),
          opened(false),
          pending_scan(),
          pending_timestamp(0),
          has_pending_scan(false)
    {
    }

    std::mutex mutex;
    std::unique_ptr<pavo::pavo_driver> driver;
    bool opened;
    std::vector<pavo_response_scan_t> pending_scan;
    uint32_t pending_timestamp;
    bool has_pending_scan;
};

namespace
{
    thread_local char last_error[1024] = {};

    void SetError(const char* message)
    {
        const char* text = message ? message : "Unknown error";
        const size_t text_length = std::strlen(text);
        const size_t length = text_length < sizeof(last_error) - 1
            ? text_length
            : sizeof(last_error) - 1;
        std::memcpy(last_error, text, length);
        last_error[length] = '\0';
    }

    int32_t Fail(int32_t status, const char* message)
    {
        SetError(message);
        return status;
    }

    int32_t SdkResult(bool succeeded, const char* message)
    {
        return succeeded ? PAVO_STATUS_OK : Fail(PAVO_STATUS_SDK_ERROR, message);
    }

    template <typename Operation>
    int32_t WithHandle(PavoHandle* handle, bool require_open, Operation operation)
    {
        if (!handle)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Handle is null");

        try
        {
            std::lock_guard<std::mutex> lock(handle->mutex);
            if (require_open && !handle->opened)
                return Fail(PAVO_STATUS_NOT_OPEN, "LiDAR is not open");

            const int32_t status = operation(*handle);
            if (status == PAVO_STATUS_OK)
                last_error[0] = '\0';
            return status;
        }
        catch (const pavo_exception& error)
        {
            return Fail(PAVO_STATUS_SDK_ERROR, error.what());
        }
        catch (const std::exception& error)
        {
            return Fail(PAVO_STATUS_SDK_ERROR, error.what());
        }
        catch (...)
        {
            return Fail(PAVO_STATUS_SDK_ERROR, "Unknown native exception");
        }
    }

    template <typename Getter>
    int32_t CopyStringResult(
        PavoHandle* handle,
        char* buffer,
        uint32_t buffer_capacity,
        uint32_t* required_capacity,
        Getter getter,
        const char* error_message)
    {
        if (!required_capacity || (!buffer && buffer_capacity != 0))
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Invalid string output buffer");

        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            std::string value;
            if (!getter(*state.driver, value))
                return Fail(PAVO_STATUS_SDK_ERROR, error_message);

            const size_t required = value.size() + 1;
            if (required > (std::numeric_limits<uint32_t>::max)())
                return Fail(PAVO_STATUS_SDK_ERROR, "String is too large");

            *required_capacity = static_cast<uint32_t>(required);
            if (buffer_capacity < required)
                return Fail(PAVO_STATUS_BUFFER_TOO_SMALL, "String output buffer is too small");

            std::memcpy(buffer, value.c_str(), required);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }
}

extern "C"
{
    PavoHandle* PAVO_CALL Pavo_Create(void)
    {
        try
        {
            PavoHandle* handle = new PavoHandle();
            last_error[0] = '\0';
            return handle;
        }
        catch (const pavo_exception& error)
        {
            SetError(error.what());
        }
        catch (const std::exception& error)
        {
            SetError(error.what());
        }
        catch (...)
        {
            SetError("Unknown native exception");
        }
        return nullptr;
    }

    void PAVO_CALL Pavo_Destroy(PavoHandle* handle)
    {
        try
        {
            delete handle;
            last_error[0] = '\0';
        }
        catch (const std::exception& error)
        {
            SetError(error.what());
        }
        catch (...)
        {
            SetError("Unknown native exception");
        }
    }

    int32_t PAVO_CALL Pavo_Open(PavoHandle* handle, const char* device_ip, uint16_t device_port)
    {
        if (!handle || !device_ip || device_ip[0] == '\0')
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Handle and device IP are required");

        return WithHandle(handle, false, [&](PavoHandle& state)
        {
            if (state.opened)
                return Fail(PAVO_STATUS_SDK_ERROR, "LiDAR is already open");

            try
            {
                if (!state.driver->pavo_open(device_ip, device_port))
                {
                    state.driver->pavo_close();
                    state.has_pending_scan = false;
                    return Fail(PAVO_STATUS_SDK_ERROR, "Failed to open LiDAR");
                }
                state.opened = true;
                state.has_pending_scan = false;
                return static_cast<int32_t>(PAVO_STATUS_OK);
            }
            catch (...)
            {
                state.driver->pavo_close();
                state.has_pending_scan = false;
                throw;
            }
        });
    }

    int32_t PAVO_CALL Pavo_Close(PavoHandle* handle)
    {
        return WithHandle(handle, false, [](PavoHandle& state)
        {
            state.driver->pavo_close();
            state.opened = false;
            state.pending_scan.clear();
            state.has_pending_scan = false;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    uint32_t PAVO_CALL Pavo_GetLastError(char* buffer, uint32_t buffer_capacity)
    {
        const size_t length = std::strlen(last_error);
        const size_t required = length == 0 ? 0 : length + 1;
        if (buffer && buffer_capacity)
        {
            const size_t copied = (required < buffer_capacity) ? required : buffer_capacity;
            if (copied == required)
                std::memcpy(buffer, last_error, copied);
            else if (copied > 0)
            {
                std::memcpy(buffer, last_error, copied - 1);
                buffer[copied - 1] = '\0';
            }
        }
        return static_cast<uint32_t>(required);
    }

    int32_t PAVO_CALL Pavo_IsConnected(PavoHandle* handle, int32_t* connected)
    {
        if (!connected)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Connection output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            *connected = state.driver->is_lidar_connected() ? 1 : 0;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_GetScanFrame(
        PavoHandle* handle,
        PavoScanPoint* points,
        uint32_t point_capacity,
        uint32_t* point_count,
        uint32_t* timestamp,
        int32_t timeout_ms)
    {
        if (!point_count || !timestamp || (!points && point_capacity != 0) || timeout_ms < 0)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Invalid scan frame arguments");

        *point_count = 0;
        *timestamp = 0;
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            if (!state.has_pending_scan)
            {
                if (!state.driver->get_scanned_data_timestamp(
                        state.pending_scan, state.pending_timestamp, timeout_ms))
                {
                    return Fail(PAVO_STATUS_TIMEOUT, "Timed out waiting for a scan frame");
                }
                state.has_pending_scan = true;
            }

            if (state.pending_scan.size() > (std::numeric_limits<uint32_t>::max)())
                return Fail(PAVO_STATUS_SDK_ERROR, "SDK returned too many scan points");
            *point_count = static_cast<uint32_t>(state.pending_scan.size());
            *timestamp = state.pending_timestamp;
            if (point_capacity < state.pending_scan.size())
                return Fail(PAVO_STATUS_BUFFER_TOO_SMALL, "Scan output buffer is too small");

            for (size_t i = 0; i < state.pending_scan.size(); ++i)
            {
                points[i].angle = state.pending_scan[i].angle;
                points[i].distance = state.pending_scan[i].distance;
                points[i].intensity = state.pending_scan[i].intensity;
            }
            state.pending_scan.clear();
            state.has_pending_scan = false;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_EnableData(PavoHandle* handle, int32_t enabled)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            state.driver->enable_data(enabled != 0);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_EnableMotor(PavoHandle* handle, int32_t enabled)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            state.driver->enable_motor(enabled != 0);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_GetDeviceType(PavoHandle* handle, int32_t* device_type)
    {
        if (!device_type)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Device type output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            const int value = state.driver->get_device_type();
            if (value < 0)
                return Fail(PAVO_STATUS_SDK_ERROR, "Failed to read device type");
            *device_type = value;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_GetDeviceSerialNumber(PavoHandle* handle, uint32_t* serial_number)
    {
        if (!serial_number)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Serial number output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->get_device_sn(*serial_number), "Failed to read serial number");
        });
    }

    int32_t PAVO_CALL Pavo_GetDeviceProductNumber(PavoHandle* handle, uint32_t* product_number)
    {
        if (!product_number)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Product number output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->get_device_pn(*product_number), "Failed to read product number");
        });
    }

    int32_t PAVO_CALL Pavo_GetMotorSpeed(PavoHandle* handle, int32_t* speed)
    {
        if (!speed)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Motor speed output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            int value = 0;
            if (!state.driver->get_motor_speed(value))
                return Fail(PAVO_STATUS_SDK_ERROR, "Failed to read motor speed");
            *speed = value;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_SetMotorSpeed(PavoHandle* handle, int32_t speed)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_motor_speed(speed), "Failed to set motor speed");
        });
    }

    int32_t PAVO_CALL Pavo_GetMergeCoefficient(PavoHandle* handle, int32_t* coefficient)
    {
        if (!coefficient)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Merge coefficient output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            int value = 0;
            if (!state.driver->get_merge_coef(value))
                return Fail(PAVO_STATUS_SDK_ERROR, "Failed to read merge coefficient");
            *coefficient = value;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_SetMergeCoefficient(PavoHandle* handle, int32_t coefficient)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_merge_coef(coefficient), "Failed to set merge coefficient");
        });
    }

    int32_t PAVO_CALL Pavo_GetDegreeShift(PavoHandle* handle, int32_t* shift)
    {
        if (!shift)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Degree shift output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            int value = 0;
            if (!state.driver->get_degree_shift(value))
                return Fail(PAVO_STATUS_SDK_ERROR, "Failed to read degree shift");
            *shift = value;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_SetDegreeShift(PavoHandle* handle, int32_t shift)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_degree_shift(shift), "Failed to set degree shift");
        });
    }

    int32_t PAVO_CALL Pavo_GetDegreeScope(PavoHandle* handle, int32_t* minimum, int32_t* maximum)
    {
        if (!minimum || !maximum)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Degree scope output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            state.driver->get_degree_scope(*minimum, *maximum);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_SetDegreeScope(PavoHandle* handle, int32_t minimum, int32_t maximum)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_degree_scope(minimum, maximum), "Failed to set degree scope");
        });
    }

    int32_t PAVO_CALL Pavo_GetMode(PavoHandle* handle, int32_t* mode)
    {
        if (!mode)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Mode output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            pavo_mode_t value = Unknown;
            state.driver->get_pavo_mode(value);
            *mode = static_cast<int32_t>(value);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_SetMode(PavoHandle* handle, int32_t mode)
    {
        if (mode < PAVO_MODE_NORMAL || mode > PAVO_MODE_ANTI)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Mode value is invalid");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(
                state.driver->set_pavo_mode(static_cast<pavo_mode_t>(mode)),
                "Failed to set mode");
        });
    }

    int32_t PAVO_CALL Pavo_EnableTailFilter(PavoHandle* handle, int32_t method)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            state.driver->enable_tail_filter(method);
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }

    int32_t PAVO_CALL Pavo_Reset(PavoHandle* handle)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->reset(), "Failed to reset LiDAR");
        });
    }

    int32_t PAVO_CALL Pavo_GetDestinationIp(
        PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity)
    {
        return CopyStringResult(handle, buffer, buffer_capacity, required_capacity,
            [](pavo::pavo_driver& driver, std::string& value) { return driver.get_dest_ip(value); },
            "Failed to read destination IP");
    }

    int32_t PAVO_CALL Pavo_SetDestinationIp(PavoHandle* handle, const char* ip)
    {
        if (!ip)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Destination IP is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_dest_ip(ip), "Failed to set destination IP");
        });
    }

    int32_t PAVO_CALL Pavo_GetDestinationPort(PavoHandle* handle, uint16_t* port)
    {
        if (!port)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Destination port output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->get_dest_port(*port), "Failed to read destination port");
        });
    }

    int32_t PAVO_CALL Pavo_SetDestinationPort(PavoHandle* handle, uint16_t port)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_dest_port(port), "Failed to set destination port");
        });
    }

    int32_t PAVO_CALL Pavo_GetLidarIp(
        PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity)
    {
        return CopyStringResult(handle, buffer, buffer_capacity, required_capacity,
            [](pavo::pavo_driver& driver, std::string& value) { return driver.get_lidar_ip(value); },
            "Failed to read LiDAR IP");
    }

    int32_t PAVO_CALL Pavo_SetLidarIp(PavoHandle* handle, const char* ip)
    {
        if (!ip)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "LiDAR IP is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_lidar_ip(ip), "Failed to set LiDAR IP");
        });
    }

    int32_t PAVO_CALL Pavo_GetLidarPort(PavoHandle* handle, uint16_t* port)
    {
        if (!port)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "LiDAR port output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->get_lidar_port(*port), "Failed to read LiDAR port");
        });
    }

    int32_t PAVO_CALL Pavo_SetLidarPort(PavoHandle* handle, uint16_t port)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->set_lidar_port(port), "Failed to set LiDAR port");
        });
    }

    int32_t PAVO_CALL Pavo_ApplyNetworkConfig(PavoHandle* handle)
    {
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(state.driver->apply_net_config(), "Failed to apply network configuration");
        });
    }

    int32_t PAVO_CALL Pavo_SetNetworkConfig(
        PavoHandle* handle,
        const char* destination_ip,
        uint16_t destination_port,
        const char* lidar_ip,
        uint16_t lidar_port)
    {
        if (!destination_ip || !lidar_ip)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Network IP is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            return SdkResult(
                state.driver->set_net_config(destination_ip, destination_port, lidar_ip, lidar_port),
                "Failed to set network configuration");
        });
    }

    int32_t PAVO_CALL Pavo_GetFirmwareVersion(
        PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity)
    {
        return CopyStringResult(handle, buffer, buffer_capacity, required_capacity,
            [](pavo::pavo_driver& driver, std::string& value) { return driver.get_fw_ver(value); },
            "Failed to read firmware version");
    }

    int32_t PAVO_CALL Pavo_GetDeviceErrorCode(PavoHandle* handle, int32_t* error_code)
    {
        if (!error_code)
            return Fail(PAVO_STATUS_INVALID_ARGUMENT, "Device error code output is null");
        return WithHandle(handle, true, [&](PavoHandle& state)
        {
            int value = 0;
            if (!state.driver->get_error_code(value))
                return Fail(PAVO_STATUS_SDK_ERROR, "Failed to read device error code");
            *error_code = value;
            return static_cast<int32_t>(PAVO_STATUS_OK);
        });
    }
}
