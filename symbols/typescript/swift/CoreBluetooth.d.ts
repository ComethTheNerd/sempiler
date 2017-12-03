/** 
BSD License

For Sempiler software

Copyright (c) 2017-present, Quantum Commune Ltd. All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

 * Neither the name Quantum Commune nor the names of its contributors may be used to
   endorse or promote products derived from this software without specific
   prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

/// <reference no-default-lib="true"/>

declare let CBAdvertisementDataLocalNameKey : string;
declare let CBAdvertisementDataServiceUUIDsKey : string;
declare let CBCentralManagerScanOptionAllowDuplicatesKey: string;

declare class CBATTErr {
    public errorCode : Int;
    public errorUserInfo : { [_ : String] : any }
    public localizedDescription : String;

    public static attributeNotFound : CBATTErr.Code;
    public static attributeNotLong : CBATTErr.Code;
    public static errorDomain : String;
    public static insufficientAuthentication : CBATTErr.Code;
    public static insufficientAuthorization : CBATTErr.Code;
    public static insufficientEncryption : CBATTErr.Code;
    public static insufficientEncryptionKeySize : CBATTErr.Code;
    public static insufficientResources : CBATTErr.Code;
    public static invalidAttributeValueLength : CBATTErr.Code;
    public static invalidHandle : CBATTErr.Code;
    public static invalidOffset : CBATTErr.Code;
    public static invalidPdu : CBATTErr.Code;
    public static prepareQueueFull : CBATTErr.Code;
    public static readNotPermitted : CBATTErr.Code;
    public static requestNotSupported : CBATTErr.Code;
    public static success : CBATTErr.Code;
    public static unlikelyError : CBATTErr.Code;
    public static unsupportedGroupType : CBATTErr.Code;
    public static writeNotPermitted : CBATTErr.Code;
}

declare module CBATTErr
{
    enum Code
    {
        success,
        invalidHandle,
        readNotPermitted,
        writeNotPermitted,
        invalidPdu,
        insufficientAuthentication,
        requestNotSupported,
        invalidOffset,
        insufficientAuthorization,
        prepareQueueFull,
        attributeNotFound,
        attributeNotLong,
        insufficientEncryptionKeySize,
        invalidAttributeValueLength,
        unlikelyError,
        insufficientEncryption,
        unsupportedGroupType,
        insufficientResources
    }
}

declare class CBManager extends NSObject {
    state : CBManagerState
}

declare class CBPeer extends NSObject
{
    public identifier : UUID;
}

declare class CBCentral extends CBPeer
{
    public maximumUpdateValueLength : Int;
}

declare class CBCentralManager extends CBManager {
    constructor(delegate : label<'delegate'> | opt<CBCentralManagerDelegate>, queue : label<'queue'> | opt<DispatchQueue>)

    isScanning : boolean;
    scanForPeripherals(withServices?: label<'withServices'> | CBUUID[], options? : label<'options'> | { [key : string] : any})
    stopScan()
    connect(_ : CBPeripheral, options? : label<'options'> | { [key : string] : any }) : void
    cancelPeripheralConnection(peripheral : CBPeripheral) : void;
}

declare interface CBCentralManagerDelegate {
    centralManagerDidUpdateState(_: CBCentralManager) : void
    // centralManager(_: CBCentralManager, willRestoreState: { [key : string] : any }) : terminator | void
    centralManager?(_ : CBCentralManager, didDiscover : label<'didDiscover'> | CBPeripheral, advertisementData : { [key : string] : any } , rssi : NSNumber) : void
    centralManager?(_ : CBCentralManager, didConnect : label<'didConnect'> | CBPeripheral) : void
    centralManager?(_: CBCentralManager, didFailToConnect: label<'didFailToConnect'> | CBPeripheral, error: Error?) : void
    // centralManager(_: CBCentralManager, didDisconnectPeripheral: CBPeripheral, error: Error?) : terminator | void
}

declare class CBUUID extends string {

    constructor(string : label<'string'> | string)

};

// https://developer.apple.com/documentation/corebluetooth/cbperipheral
declare class CBPeripheral {
    name? : string;
    delegate? : CBPeripheralDelegate;
    services? : CBService[];
    writeValue(_ : Data, for_ : label<'for'> | CBCharacteristic, type : label<'type'> | CBCharacteristicWriteType)
    discoverServices(_? : CBUUID[]) : void
    discoverIncludedServices(_? : CBUUID[], _for : label<'for'> | CBService) : void
    discoverCharacteristics(characteristicUUIDs : opt<CBUUID[]>, service : label<'for'> | CBService) : void;

    readValue(_ : label<'for'> | CBCharacteristic)
    writeValue(_ : Data, characteristic : label<'for'> | CBCharacteristic, type : CBCharacteristicWriteType);
    
    setNotifyValue(_ : Bool, characteristic : label<'for'> | CBCharacteristic)
}

declare interface CBPeripheralDelegate
{
    peripheral?(peripheral : CBPeripheral, error : label<'didDiscoverServices'> | opt<Error>) : void;
    peripheral?(peripheral : CBPeripheral, service : label<'didDiscoverCharacteristicsFor'> | CBService, error : opt<Error>) : void;
    peripheral?(peripheral : CBPeripheral, characteristic : label<'didUpdateValueFor'> | CBCharacteristic, error : opt<Error>) : void;
    peripheral?(peripheral : CBPeripheral, descriptor : label<'didWriteValueFor'> | CBDescriptor, error : opt<Error>) : void;
}

declare class CBPeripheralManager extends CBManager {

    constructor(delegate : label<'delegate'> | opt<CBPeripheralManagerDelegate>, queue : label<'queue'> | opt<DispatchQueue>);

    startAdvertising(_ : opt<{ [key : string] : any }>) : void;
    add(_ : CBMutableService) : void;
    remove(_ : CBMutableService) : void;
    removeAllServices() : void;

    respond(to : label<'to'> | CBAttRequest, withResul : label<'withResult'> | CBATTErr.Code)
}

declare interface CBPeripheralManagerDelegate {
    peripheralManagerDidUpdateState(_ : CBPeripheralManager)
    peripheralManager?(_ : CBPeripheralManager, didReceiveRead : label<'didReceiveRead'> | CBATTRequest)
    peripheralManager?(_ : CBPeripheralManager, didReceiveWrite : label<'didReceiveWrite'> | CBATTRequest[])
}

declare class CBATTRequest 
{
    public central : CBCentral;
    public characteristic : CBCharacteristic;
    public value : opt<Data>;
    public offset : Int;
}

declare class CBService {
    characteristics : opt<CBCharacteristic[]>
}

declare class CBMutableService extends CBService 
{
    public constructor(type : label<'type'> | CBUUID, primary : label<'primary'> | Bool)
}

declare class CBCharacteristic {
    uuid : CBUUID;
    service : CBService;
    value : opt<Data>
}

declare class CBMutableCharacteristic extends CBCharacteristic {
    public constructor(type : label<'type'> | CBUUID, properties : label<'properties'> | CBCharacteristicProperties, value : label<'value'> | opt<Data>, permissions : label<'permissions'> | CBAttributePermissions)
}

declare class CBCharacteristicProperties implements OptionSet
{
    public constructor(rawValue : label<'rawValue'> | UInt);

    public static broadcast : CBCharacteristicProperties;
    public static read : CBCharacteristicProperties;
    public static writeWithoutResponse : CBCharacteristicProperties;
    public static write : CBCharacteristicProperties;
    public static notify : CBCharacteristicProperties;
    public static indicate : CBCharacteristicProperties;
    public static authenticatedSignedWrites : CBCharacteristicProperties;
    public static extendedProperties : CBCharacteristicProperties;
    public static notifyEncryptionRequired : CBCharacteristicProperties;
    public static indicateEncryptionRequired : CBCharacteristicProperties;
}

declare class CBAttributePermissions implements OptionSet
{
    public constructor(rawValue : label<'rawValue'> | UInt);
    
    public static readable : CBAttributePermissions;
    public static writeable : CBAttributePermissions;
    public static readEncryptionRequired : CBAttributePermissions;
    public static writeEncryptionRequired : CBAttributePermissions;
}

declare enum CBManagerState {
    poweredOn,
    poweredOff
    // ... todo others
}

declare class CBDescriptor {

}

declare enum CBCharacteristicWriteType {
    withResponse,
    withoutResponse
}