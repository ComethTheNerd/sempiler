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

declare class CBManager {
    state : CBManagerState
}

declare class CBCentralManager extends CBManager {
    constructor(delegate : label<'delegate'> | opt<CBCentralManagerDelegate>, queue : label<'queue'> | opt<DispatchQueue>)

    isScanning : boolean;
    scanForPeripherals(withServices?: label<'withServices'> | CBUUID[], options? : label<'options'> | { [key : string] : any})
    stopScan()
    connect(_ : CBPeripheral, options? : label<'options'> | { [key : string] : any }) : void
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
    peripheral(peripheral : CBPeripheral, error : label<'didDiscoverServices'> | opt<Error>) : void;
    peripheral(peripheral : CBPeripheral, service : label<'didDiscoverCharacteristicsFor'> | CBService, error : opt<Error>) : void;
    peripheral(peripheral : CBPeripheral, characteristic : label<'didUpdateValueFor'> | CBCharacteristic, error : opt<Error>) : void;
    peripheral(peripheral : CBPeripheral, descriptor : label<'didWriteValueFor'> | CBDescriptor, error : opt<Error>) : void;
}

declare class CBPeripheralManager extends CBManager {

    constructor(delegate : label<'delegate'> | opt<CBPeripheralManagerDelegate>, queue : label<'queue'> | opt<DispatchQueue>);
}

declare interface CBPeripheralManagerDelegate {
    peripheralManagerDidUpdateState(_ : CBPeripheralManager)
}

declare class CBService {
    characteristics : opt<CBCharacteristic[]>
}

declare class CBCharacteristic {
    uuid : CBUUID;
    service : CBService;
    value : opt<Data>
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