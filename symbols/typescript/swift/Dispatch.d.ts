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

declare type dispatch_time_t = uint64_t;

declare class DispatchObject 
{
    public activate() : void;
    public resume() : void;
    public setTarget(queue : label<'queue'> | opt<DispatchQueue>) : void;
    public suspend() : void;
}

declare class DispatchGroup extends DispatchObject
{
    public constructor();

    public enter();
    public leave();
    public notify(qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, queue : label<'queue'> | DispatchQueue, execute : label<'execute'> | (() => void))
    public notify(queue : label<'queue'> | DispatchQueue, work : label<'work'> | DispatchWorkItem)
    public wait();
    public wait(timeout : label<'timeout'> | DispatchTime);
    public wait(wallTimeout : label<'wallTimeout'> | DispatchWallTime);
}

declare class DispatchQoS 
{
    public constructor(qosClass : label<'qosClass'> | DispatchQoS.QoSClass, releativePriority : label<'relativePriority'> | Int)

    public qosClass : DispatchQoS.QoSClass;
    public relativePriority : Int;

    public static background : DispatchQoS;
    public static default : DispatchQoS;
    public static unspecified : DispatchQoS;
    public static userInitiated : DispatchQoS;
    public static userInteractive : DispatchQoS;
    public static utility : DispatchQoS;
}

declare module DispatchQoS
{
    enum QoSClass
    {
        userInteractive,
        userInitiated,
        default,
        utility,
        background,
        unspecified
    }

}

declare class DispatchWorkItem
{
    public constructor(qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, block : label<'block'> | (() => void));

    public isCancelled : Bool;
    
    public cancel();
    public notify(qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, queue : label<'queue'> | DispatchQueue, execute : label<'execute'> | (() => void))
    public notify(queue : label<'queue'> | DispatchQueue, execute : label<'execute'> | (() => void))
    public perform();
    public wait();
    public wait(timeout : label<'timeout'> | DispatchTime);
    public wait(wallTimeout : label<'wallTimeout'> | DispatchWallTime);
}

declare class DispatchWorkItemFlags implements OptionSet
{
    public constructor();
    //public constructor<S>(_ : S);
    public constructor(...arrayLiteral : label<'arrayLiteral'> | DispatchWorkItemFlags[])
    public constructor(rawValue : label<'rawValue'> | UInt)

    public isEmpty : Bool;
    public rawValue : UInt;

    public static assignCurrentContext : DispatchWorkItemFlags;
    public static barrier : DispatchWorkItemFlags;
    public static detached : DispatchWorkItemFlags;
    public static enforceQoS : DispatchWorkItemFlags;
    public static inheritQoS : DispatchWorkItemFlags;
    public static noQoS : DispatchWorkItemFlags;

    public contains(_ : DispatchWorkItemFlags) : Bool;
    public formIntersection(_ : DispatchWorkItemFlags) : void;
    public formSymmetricDifference(_ : DispatchWorkItemFlags) : void;
    public formUnion(_ : DispatchWorkItemFlags) : void;
    public insert(_ : DispatchWorkItemFlags) : void;
    public intersection(_ : DispatchWorkItemFlags) : DispatchWorkItemFlags;
    public isDisjoint(_ : label<'with'> | DispatchWorkItemFlags) : Bool;
    public isStrictSubset(_ : label<'of'> | DispatchWorkItemFlags) : Bool;
    public isStrictSuperset(_ : label<'of'> | DispatchWorkItemFlags) : Bool;
    public isSubset(_ : label<'of'> | DispatchWorkItemFlags) : Bool;
    public isSuperset(_ : label<'of'> | DispatchWorkItemFlags) : Bool;
    public remove(_ : DispatchWorkItemFlags) : void;
    public subtract(_ : DispatchWorkItemFlags) : void;
    public subtracting(_ : DispatchWorkItemFlags) : DispatchWorkItemFlags;
    public symmetricDifference(_ : DispatchWorkItemFlags) : DispatchWorkItemFlags;
    public union(_ : DispatchWorkItemFlags) : DispatchWorkItemFlags;
    public update(_ : label<'with'> | DispatchWorkItemFlags) : void;
}

declare class DispatchTime
{
    public constructor(uptimeNanoseconds : label<'uptimeNanoseconds'> | UInt64)

    public rawValue : dispatch_time_t;
    public uptimeNanosecond : UInt64;

    public static distantFuture : DispatchTime;

    public static now() : DispatchTime;
}

declare class DispatchWallTime
{
    public constructor(timespec : label<'timespec'> | timespec);

    public rawValue : dispatch_time_t;

    public static distantFuture : DispatchWallTime;

    public static now() : DispatchWallTime;
}

declare class DispatchSpecificKey<T>
{
    public constructor();
}


declare const DISPATCH_QUEUE_SERIAL;
declare const DISPATCH_QUEUE_CONCURRENT;
declare type __OS_dispatch_queue_attr = any;

declare class DispatchQueue 
{
    public constructor(__label : label<'__label'> | opt<UnsafePointer<Int8>>, attr : label<'attr'> | opt<__OS_dispatch_queue_attr>)
    public constructor(__label : label<'__label'> |opt<UnsafePointer<Int8>>, attr : label<'attr'> |opt<__OS_dispatch_queue_attr>, queue : label<'queue'> |opt<DispatchQueue>)
    public constructor(label : label<'label'> |String, qos : label<'qos'> | DispatchQoS, attributes : label<'attributes'> | DispatchQueue.Attributes, autoreleaseFrequency : label<'autoreleaseFrequency'> | DispatchQueue.AutoreleaseFrequency, target : label<'target'> | opt<DispatchQueue>)

    public label : String;
    public qos : DispatchQoS;

    public static main : DispatchQueue;
    public static concurrentPerform(iterations : label<'iterations'> | Int, execute : label<'execute'> | ((_ : Int) => void)) : void;
    public static getSpecific<T>(key : label<'key'> | DispatchSpecificKey<T>) : opt<T>
    public static global(priority : label<'priority'> | DispatchQueue.GlobalQueuePriority) : DispatchQueue;
    public static global(qos : label<'qos'> | DispatchQoS.QoSClass) : DispatchQueue;

    public sync(execute : label<'execute'> | (() => void)) : void;
    public async(execute : label<'execute'> | (() => void)) : void;
    public async(execute : label<'execute'> | DispatchWorkItem) : void;
    public async(group : label<'group'> | DispatchGroup, execute : label<'execute'> | DispatchWorkItem) : void;
    public async(group : label<'group'> | opt<DispatchGroup>, qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, execute : label<'execute'> | (() => void)) : void;
    public asyncAfter(deadline : label<'deadline'> | DispatchTime, execute : label<'execute'> | DispatchWorkItem) : void;
    public asyncAfter(deadline : label<'deadline'> | DispatchTime, qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, execute : label<'execute'> | (() => void)) : void;
    public asyncAfter(wallDeadline : label<'wallDeadline'> | DispatchWallTime, execute : label<'execute'> | DispatchWorkItem) : void;
    public asyncAfter(wallDeadline : label<'wallDeadline'> | DispatchWallTime, qos : label<'qos'> | DispatchQoS, flags : label<'flags'> | DispatchWorkItemFlags, execute : label<'execute'> | (() => void)) : void;

    public getSpecfic<T>(key : label<'key'> | DispatchSpecificKey<T>) : opt<T>
    public setSpecific<T>(key : label<'key'> | DispatchSpecificKey<T>, value : label<'value'> | opt<T>) : void;

    public sync<T>(execute : label<'execute'> | (() => T)) : void;
    public sync(execute : label<'execute'> | DispatchWorkItem) : void;
    public sync<T>(flags : label<'flags'> | DispatchWorkItemFlags, execute : label<'execute'> | (() => T)) : void;
}

declare module DispatchQueue 
{
    class Attributes implements OptionSet
    {
        public constructor();
        //public constructor<S>(_ : S);
        public constructor(...arrayLiteral : label<'arrayLiteral'> | DispatchQueue.Attributes[])
        public constructor(rawValue : label<'rawValue'> | UInt64)

        public isEmpty : Bool;
        public rawValue : UInt64

        public static concurrent : DispatchQueue.Attributes;
        public static initiallyInactive : DispatchQueue.Attributes;

        public contains(_ : DispatchQueue.Attributes) : Bool;
        public formIntersection(_ : DispatchQueue.Attributes) : void;
        public formSymmetricDifference(_ : DispatchQueue.Attributes) : void;
        public formUnion(_ : DispatchQueue.Attributes) : void;
        public insert(_ : DispatchQueue.Attributes) : void;
        public intersection(_ : DispatchQueue.Attributes) : DispatchQueue.Attributes;
        public isDisjoint(_ : label<'with'> | DispatchQueue.Attributes) : Bool;
        public isStrictSubset(_ : label<'of'> | DispatchQueue.Attributes) : Bool;
        public isStrictSuperset(_ : label<'of'> | DispatchQueue.Attributes) : Bool;
        public isSubset(_ : label<'of'> | DispatchQueue.Attributes) : Bool;
        public isSuperset(_ : label<'of'> | DispatchQueue.Attributes) : Bool;
        public remove(_ : DispatchQueue.Attributes) : void;
        public subtract(_ : DispatchQueue.Attributes) : void;
        public subtracting(_ : DispatchQueue.Attributes) : DispatchQueue.Attributes;
        public symmetricDifference(_ : DispatchQueue.Attributes) : DispatchQueue.Attributes;
        public union(_ : DispatchQueue.Attributes) : DispatchQueue.Attributes;
        public update(_ : label<'with'> | DispatchQueue.Attributes) : void;
    }   

    enum AutoreleaseFrequency 
    {
        inherit,
        never,
        workItem
    }

    enum GlobalQueuePriority
    {
        background,
        default,
        high,
        low
    }
}