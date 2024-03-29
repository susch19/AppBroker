﻿namespace AppBroker.PainlessMesh;

public enum PackageType
{
    DROP = 3,
    SYNC_PACKAGE = 4,
    TIME_SYNC = 4,
    NODE_SYNC_REQUEST = 5,
    NODE_SYNC_REPLY = 6,
    BROADCAST = 8,  //application data for everyone
    SINGLE = 9,   //application data for a single node
    OTA_ANNOUNCE = 10,
    OTA_REQUEST = 11,
    OTA_REPLY = 12,
    BRIDGE = 255,

};
