import { ChildProcess } from 'child_process';
import { ReadLine, createInterface } from 'readline';
import { EventEmitter } from 'events';
import { Message } from './diagnostics';

export type EventHandler<T = any> = (data: T) => void;

export interface Packet {
    id: string;
    type: Type;
}

export interface Result<T = any> {
    messages : MessageCollection;
    value? : T;
}

export interface MessageCollection
{
    infos? : Message[];
    warnings? : Message[];
    errors? : Message[];
}

export interface Request extends Packet {
    type: Type.Request;
    command: string;
    data: any;
}

export type Data = Result | Error;

export interface Response extends Packet {
    type: Type.Response;
    requestID: string;
    ok: boolean;
    data: Data;
}

export interface Event extends Packet {
    type: Type.Event;
    event: string;
    data: Data;
}

// export interface Error extends Packet 
// {
//     description : string;
// }

export enum Type {
    Request = 'request',
    Response = 'response',
    Event = 'event',
    // Error = 'error',
}