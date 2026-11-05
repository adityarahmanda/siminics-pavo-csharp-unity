#pragma once

#include <stdint.h>

#if defined(_WIN32)
#if defined(PAVOSHARPWRAPPER_EXPORTS)
#define PAVO_API __declspec(dllexport)
#else
#define PAVO_API __declspec(dllimport)
#endif
#define PAVO_CALL __cdecl
#else
#define PAVO_API
#define PAVO_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct PavoHandle PavoHandle;

typedef struct PavoScanPoint
{
    uint16_t angle;
    uint16_t distance;
    uint16_t intensity;
} PavoScanPoint;

typedef enum PavoStatus
{
    PAVO_STATUS_OK = 0,
    PAVO_STATUS_TIMEOUT = 1,
    PAVO_STATUS_BUFFER_TOO_SMALL = 2,
    PAVO_STATUS_INVALID_ARGUMENT = -1,
    PAVO_STATUS_NOT_OPEN = -2,
    PAVO_STATUS_SDK_ERROR = -3
} PavoStatus;

typedef enum PavoMode
{
    PAVO_MODE_UNKNOWN = -1,
    PAVO_MODE_NORMAL = 0,
    PAVO_MODE_ECHO = 1,
    PAVO_MODE_ANTI = 2
} PavoMode;

PAVO_API PavoHandle* PAVO_CALL Pavo_Create(void);
PAVO_API void PAVO_CALL Pavo_Destroy(PavoHandle* handle);
PAVO_API int32_t PAVO_CALL Pavo_Open(PavoHandle* handle, const char* device_ip, uint16_t device_port);
PAVO_API int32_t PAVO_CALL Pavo_Close(PavoHandle* handle);
/* Returns the required character count, including the null terminator, or zero when no error is set. */
PAVO_API uint32_t PAVO_CALL Pavo_GetLastError(char* buffer, uint32_t buffer_capacity);

PAVO_API int32_t PAVO_CALL Pavo_IsConnected(PavoHandle* handle, int32_t* connected);
/*
 * timeout_ms == 0 waits indefinitely. A too-small buffer does not consume the frame;
 * point_count reports the required capacity so the caller can retry.
 */
PAVO_API int32_t PAVO_CALL Pavo_GetScanFrame(
    PavoHandle* handle,
    PavoScanPoint* points,
    uint32_t point_capacity,
    uint32_t* point_count,
    uint32_t* timestamp,
    int32_t timeout_ms);

PAVO_API int32_t PAVO_CALL Pavo_EnableData(PavoHandle* handle, int32_t enabled);
PAVO_API int32_t PAVO_CALL Pavo_EnableMotor(PavoHandle* handle, int32_t enabled);
PAVO_API int32_t PAVO_CALL Pavo_GetDeviceType(PavoHandle* handle, int32_t* device_type);
PAVO_API int32_t PAVO_CALL Pavo_GetDeviceSerialNumber(PavoHandle* handle, uint32_t* serial_number);
PAVO_API int32_t PAVO_CALL Pavo_GetDeviceProductNumber(PavoHandle* handle, uint32_t* product_number);

PAVO_API int32_t PAVO_CALL Pavo_GetMotorSpeed(PavoHandle* handle, int32_t* speed);
PAVO_API int32_t PAVO_CALL Pavo_SetMotorSpeed(PavoHandle* handle, int32_t speed);
PAVO_API int32_t PAVO_CALL Pavo_GetMergeCoefficient(PavoHandle* handle, int32_t* coefficient);
PAVO_API int32_t PAVO_CALL Pavo_SetMergeCoefficient(PavoHandle* handle, int32_t coefficient);
PAVO_API int32_t PAVO_CALL Pavo_GetDegreeShift(PavoHandle* handle, int32_t* shift);
PAVO_API int32_t PAVO_CALL Pavo_SetDegreeShift(PavoHandle* handle, int32_t shift);
PAVO_API int32_t PAVO_CALL Pavo_GetDegreeScope(PavoHandle* handle, int32_t* minimum, int32_t* maximum);
PAVO_API int32_t PAVO_CALL Pavo_SetDegreeScope(PavoHandle* handle, int32_t minimum, int32_t maximum);
PAVO_API int32_t PAVO_CALL Pavo_GetMode(PavoHandle* handle, int32_t* mode);
PAVO_API int32_t PAVO_CALL Pavo_SetMode(PavoHandle* handle, int32_t mode);
PAVO_API int32_t PAVO_CALL Pavo_EnableTailFilter(PavoHandle* handle, int32_t method);
PAVO_API int32_t PAVO_CALL Pavo_Reset(PavoHandle* handle);

PAVO_API int32_t PAVO_CALL Pavo_GetDestinationIp(
    PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity);
PAVO_API int32_t PAVO_CALL Pavo_SetDestinationIp(PavoHandle* handle, const char* ip);
PAVO_API int32_t PAVO_CALL Pavo_GetDestinationPort(PavoHandle* handle, uint16_t* port);
PAVO_API int32_t PAVO_CALL Pavo_SetDestinationPort(PavoHandle* handle, uint16_t port);
PAVO_API int32_t PAVO_CALL Pavo_GetLidarIp(
    PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity);
PAVO_API int32_t PAVO_CALL Pavo_SetLidarIp(PavoHandle* handle, const char* ip);
PAVO_API int32_t PAVO_CALL Pavo_GetLidarPort(PavoHandle* handle, uint16_t* port);
PAVO_API int32_t PAVO_CALL Pavo_SetLidarPort(PavoHandle* handle, uint16_t port);
PAVO_API int32_t PAVO_CALL Pavo_ApplyNetworkConfig(PavoHandle* handle);
PAVO_API int32_t PAVO_CALL Pavo_SetNetworkConfig(
    PavoHandle* handle,
    const char* destination_ip,
    uint16_t destination_port,
    const char* lidar_ip,
    uint16_t lidar_port);

PAVO_API int32_t PAVO_CALL Pavo_GetFirmwareVersion(
    PavoHandle* handle, char* buffer, uint32_t buffer_capacity, uint32_t* required_capacity);
PAVO_API int32_t PAVO_CALL Pavo_GetDeviceErrorCode(PavoHandle* handle, int32_t* error_code);

#ifdef __cplusplus
}
#endif
