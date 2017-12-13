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

declare class CLLocationManager extends NSObject
{
    public requestWhenInUseAuthorization() : void;
    public requestAlwaysAuthorization() : void;

    public static authorizationStatus() : CLAuthorizationStatus;
    public static locationServicesEnabled() : Bool;
    public static deferredLocationUpdatsAvailable() : Bool;
    public static significantLocationChangeMonitoringAvailable() : Bool;
    public static headingAvailable() : Bool;
    //public static isMonitoringAvailable(_for : label<'for'> | AnyClass)
    public static isRangingAvailable() : Bool;

    public delegate : opt<CLLocationManagerDelegate>;
    
    public startUpdatingLocation() : void;
    public stopUpdatingLocation() : void;
    public requestLocation() : void;

    public pausesLocationUpdatesAutomatically : Bool;
    public allowsBackgroundLocationUpdates : Bool;
    public showBackgroundLocationIndicator : Bool;
    public distanceFilter : CLLocationDistance;
    public desiredAccuracy : CLLocationAccuracy;
    public activityType : CLActivityType;

    public startMonitoringSignificantLocationChanges() : void;
    public stopMonitoringSignificantLocationChanges() : void;

    public startUpdatingHeading() : void;
    public stopUpdatingHeading() : void;
    
    public dismissHeadingCalibrationDisplay() : void;
    
    public headingFilter : CLLocationDegrees;
    public headingOrientation : CLDeviceOrientation;

    public startMonitoring(_for : label<'for'> | CLRegion);
    public stopMonitoring(_for : label<'for'> | CLRegion);

    public monitoredRegions : Set<CLRegion>;

    public maximumRegionMonitoringDistance : CLLocationDistance;

    public startRangingBeacons(_in : label<'in'> | CLBeaconRegion);
    public stopRangingBeacons(_in : label<'in'> | CLBeaconRegion);

    public requestState(_for : label<'for'> | CLRegion);
    public rangedRegions : Set<CLRegion>;

    public startMonitoringVisits() : void;
    public stopMonitoringVisits() : void;

    public allowDeferredLocationUpdates(untilTraveled : label<'untilTraveled'> | CLLocationDistance, timeout : TimeInterval) : void;
    public disallowDeferredLocationUpdates() : void;

    public location : opt<CLLocation>;
    public heading : opt<CLHeading>;

    public kCLDistanceFilterNone : CLLocationDistance;
    public kCLHeadingFilterNone : CLLocationDegrees;
    public CLLocationDistanceMax : CLLocationDistance;
    public CLTimeIntervalMax : TimeInterval;

    public purpose : opt<String>;
    public static regionMonitoringAvailable() : void;
    public static regionMonitoringEnabled() : void;

}

declare type CLLocationAccuracy = Double;
declare type CLLocationDegrees = Double;
declare type CLLocationDistance = Double;
declare type CLLocationDirection = Double;


declare const kCLLocationAccuracyBestForNavigation: CLLocationAccuracy

declare const kCLLocationAccuracyBest: CLLocationAccuracy

declare const kCLLocationAccuracyNearestTenMeters: CLLocationAccuracy

declare const kCLLocationAccuracyHundredMeters: CLLocationAccuracy

declare const kCLLocationAccuracyKilometer: CLLocationAccuracy

declare const kCLLocationAccuracyThreeKilometers: CLLocationAccuracy


declare enum CLAuthorizationStatus
{
    notDetermined,
    restricted,
    denied,
    authorizedAlways,
    authorizedWhenInUse
}

declare interface CLLocationManagerDelegate extends NSObjectProtocol
{
    public locationManager?(_ : CLLocationManager, didUpdateLocations : label<'didUpdateLocations'> | CLLocation[]) : void;
    public locationManager?(_ : CLLocationManager, didFailWithError : label<'didFailWithError'> | Error) : void;
    public locationManager?(_ : CLLocationManager, didFinishDeferredUpdatesWithError : label<'didFinishDeferredUpdatesWithError'> | opt<Error>) : void;
    public locationManager?(_ : CLLocationManager, didUpdateTo : label<'didUpdateTo'> | CLLocation) : void;
    public locationManager?(_ : CLLocationManager, didEnterRegion : label<'didEnterRegion'> | CLRegion) : void;
    public locationManager?(_ : CLLocationManager, didExitRegion : label<'didExitRegion'> | CLRegion) : void;
    public locationManager?(_ : CLLocationManager, didDetermineState : label<'didDetermineState'> | CLRegion) : void;
    public locationManager?(_ : CLLocationManager, monitoringDidFailFor : label<'monitoringDidFailFor'> | opt<CLRegion>, withError : label<'withError'> | Error) : void;
    public locationManager?(_ : CLLocationManager, didStartMonitoringFor : label<'didStartMonitoringFor'> | CLRegion) : void;
    public locationManager?(_ : CLLocationManager, didRangeBeacons : label<'didRangeBeacons'> | CLBeacon[], _in : label<'in'> | CLBeaconRegion) : void;
    public locationManager?(_ : CLLocationManager, rangingBeaconsDidFailFor : label<'rangingBeaconsDidFailFor'> | CLBeaconRegion, withError : label<'withError'> | Error) : void;
    public locationManager?(_ : CLLocationManager, didVisit : label<'didVisit'> | CLVisit) : void;
    public locationManager?(_ : CLLocationManager, didChangeAuthorization : label<'didChangeAuthorization'> | CLAuthorizationStatus) : void;

    public locationManagerDidPauseLocationUpdates?(_ : CLLocationManager) : void;
    public locationManagerDidResumeLocationUpdates?(_ : CLLocationManager) : void;
}  

declare enum CLActivityType
{
    other,
    automotiveNavigation,
    fitness,
    otherNavigation
}

declare enum CLDeviceOrientation
{
    unknown,
    portrait,
    portraitUpsideDown,
    landscapeLeft,
    landscapeRight,
    faceUp,
    faceDown
}

declare class CLRegion extends NSObject
{
    public constructor(circularRegionWithCenter : label<'circularRegionWithCenter'> | CLLocationCoordinate2D, radius : label<'radius'> | CLLocationDistance, identifier : label<'identifier'> | String);

    public identifier : String;
    public notifyOnEntry : Bool;
    public notifyOnExit : Bool;

    public contains(_ : CLLocationCoordinate2D) : Bool;

    public center : CLLocationCoordinate2D;
    public radius : CLLocationDistance;
}

declare class CLCircularRegion extends CLRegion
{
    public constructor(center : label<'center'> | CLLocationCoordinate2D, radius : label<'radius'> | CLLocationDistance, identifier : label<'identifier'> | String);

    // public center : CLLocationCoordinates2D;
    // public radius : CLLocationDistance;

    // public contains(_ : CLLocationCoordinates2D) : Bool;
}


declare class CLBeaconRegion extends CLRegion
{
    public constructor(proximityUUID : label<'proximityUUID'> | UUID, identifier : label<'identifier'> | String);
    public constructor(proximityUUID : label<'proximityUUID'> | UUID, major : label<'major'> | CLBeaconMajorValue, identifier : label<'identifier'> | String);
    public constructor(proximityUUID : label<'proximityUUID'> | UUID, major : label<'major'> | CLBeaconMajorValue, minor : label<'minor'> | CLBeaconMinorValue, identifier : label<'identifier'> | String);
 
    public proximityUUID : UUId;
    public major : opt<NSNumber>;
    public minor : opt<NSNumber>;

    public notifyEntryStateOnDisplay : Bool;
    public peripheralData(withMeasuredPower : label<'withMeasuredPower'> | opt<NSNumber>) : NSMutableDictionary

}

declare type CLBeaconMajorValue = UInt16;
declare type CLBeaconMinorValue = UInt16;

declare enum CLRegionState
{
    unknown,
    inside,
    outside
}

declare class CLHeading
{
    public magneticHeading : CLLocationDirection;
    public trueHeading : CLLocationDirection;
    public headingAccuracy : CLLocationDirection;
    public timestamp : Date;

    public x : CLHeadingComponentValue;
    public y : CLHeadingComponentValue;
    public z : CLHeadingComponentValue;
}

declare type CLHeadingComponentValue = Double;

declare class CLLocationCoordinate2D
{
    public constructor();
    public constructor(latitude : label<'latitude'> | CLLocationDegrees, longitude : label<'longitude'> | CLLocationDegrees)

    public latitude : CLLocationDegrees;
    public longitude : CLLocationDegrees;

    public CLLocationCoordinate2DIsValid(_ : CLLocationCoordinate2D) : Bool;
    public CLLocationCoordinate2DMake(_degrees : CLLocationDegrees, _location : CLLocationDegrees) : CLLocationCoordinate2D

    public kCLLocationCoordinate2DInvalid : CLLocationCoordinate2D;
}

declare class CLLocation extends NSObject
{
    public constructor(latitude : label<'latitude'> | CLLocationDegrees, longitude : label<'longitude'> | CLLocationDegrees);
    public constructor(coordinate : label<'coordinate'> | CLLocationCoordinate2D, altitude : label<'altitude'> | CLLocationDistance, horizontalAccuracy : label<'horizontalAccuracy'> | CLLocationAccuracy, verticalAccuracy : label<'verticalAccuracy'> | CLLocationAccuracy, timestamp : label<'timestamp'> | Date);
    public constructor(coordinate : label<'coordinate'> | CLLocationCoordinate2D, altitude : label<'altitude'> | CLLocationDistance, horizontalAccuracy : label<'horizontalAccuracy'> | CLLocationAccuracy, verticalAccuracy : label<'verticalAccuracy'> | CLLocationAccuracy, course : label<'course'> | CLLocationDirection, speed : label<'speed'> | CLLocationSpeed, timestamp : label<'timestamp'> | Date);
    
    public coordinate : CLLocationCoordinate2D;
    public altitude : CLLocationDistance;
    public floor : opt<CLFloor>;
    public horizontalAccuracy : CLLocationAccuracy;
    public verticalAccuracy : CLLocationAccurancy;
    public timestamp : Date;
    
    public distance(from : label<'from'> | CLLocation) : CLLocationDistance;
    public speed : CLLocationSpeed;
    public course : CLLocationDirection;
}

declare type CLLocationSpeed = Double;

declare class CLVisit extends NSObject
{
    public coordinate : CLLocationCoordinate2D;
    public horizontalAccuracy : CLLocationAccuracy;
    public arrivalDate : Date;
    public departureDate : Date;
}

declare class CLFloor extends NSObject
{
    public level : Int;
}

declare class CLError
{
    public static deferredAccuracyTooLow : CLError.Code;
    public static deferredCanceled : CLError.Code;
    public static deferredDistanceFilter : CLError.Code;
    public static deferredFailed : CLError.Code;
    public static deferredNotUpdatingLocation : CLError.Code;
    public static denied : CLError.Code;
    public static geocodeCanceled : CLError.Code;
    public static geocodeFoundNoResult : CLError.Code;
    public static geocodeFoundPartialResult : CLError.Code;
    public static headingFailure : CLError.Code;
    public static locationUnknown : CLError.Code;
    public static network : CLError.Code;
    public static rangingFailure : CLError.Code;
    public static rangingUnavailable : CLError.Code;
    public static regionMonitoringDenied : CLError.Code;
    public static regionMonitoringFailure : CLError.Code;
    public static regionMonitoringResponseDelayed : CLError.Code;
    public static regionMonitoringSetupDelayed : CLError.Code;

    public errorCode : Int;
    public code : CLError.Code;
    public errorUserInfo : { [_ : string] : Any }
    public alternateRegion : opt<CLRegion>
    public localizedDescription : String;
    
    public static errorDomain : String;
}

declare module CLError
{
    enum Code 
    {
        locationUnknown,
        denied,
        network,
        headingFailure,
        regionMonitoringDenied,
        regionMonitoringFailure,
        regionMonitoringSetupDelayed,
        regionMonitoringResponseDelayed,
        geocodeFoundNoResult,
        geocodeFoundPartialResult,
        geocodeCanceled,
        deferredFailed,
        deferredNotUpdatingLocation,
        deferredAccuracyTooLow,
        deferredDistanceFiltered,
        deferredCanceled,
        rangingUnavailable,
        rangingFailure
    }
}

declare const kCLErrorUserInfoAlternateRegionKey;
declare const kCLErrorDomain;