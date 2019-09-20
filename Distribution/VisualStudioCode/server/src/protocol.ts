import { ChildProcess } from 'child_process';
import { ReadLine, createInterface } from 'readline';
import { CancellationToken, Disposable } from 'vscode-languageserver'
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

export type ActiveRequest = Request & { onSuccess(value: Result) : void; onError(error: Error) : void; };

export class DuplexCommunication implements Disposable 
{
    private requestID: number = 0;
    private readLine: ReadLine;
    private errorListener: EventHandler;
    private eventEmitter = new EventEmitter();
    private activeRequests: { [id: string]: ActiveRequest } = {};

    public constructor(public readonly process: ChildProcess) 
    {    
        process.stderr.on('data', 
            this.errorListener = (data: any) => this.eventEmitter.emit('error', String(data)));

        this.readLine = createInterface({
            input: process.stdout,
            output: process.stdin,
            terminal: false
        });

        this.readLine.addListener('line', this.onLineReceived.bind(this));
    }

    public dispose() {
        this.process.stderr.removeListener('data', this.errorListener);
        this.readLine.close();
    }

    public addListener(event: string, listener: EventHandler): Disposable {
        var eventEmitter = this.eventEmitter;

        eventEmitter.addListener(event, listener);

        var disposable: Disposable = {
            dispose() {
                eventEmitter.removeListener(event, listener)
            }
        }

        return disposable;
    }

    public send<T>(command: string, data: any, token?: CancellationToken): Promise<Result<T>> {
        var id = ++this.requestID + "";

        const packet: Request = {
            type: Type.Request,
            id,
            command,
            data
        };

        this.process.stdin.write(JSON.stringify(packet) + '\n');

        const promise = new Promise<Result<T>>((resolve, reject) => this.activeRequests[id] = {
            ...packet,
            onSuccess: resolve,
            onError: reject
        });

        if (token) {
            token.onCancellationRequested(() => {
                // [dho] TODO cancel request - 01/09/18
            });
        }

        return promise;
    }

    private onLineReceived(line: string) {
        line = line.trim();

        // if (line[0] !== '{') {
        //     this.eventStream.post(new ObservableEvents.OmnisharpServerMessage(line));
        //     return;
        // }

        let packet: Packet;

        try {
            packet = JSON.parse(line);
        }
        catch (err) {
            // This isn't JSON
            return;
        }

        switch (packet.type) {
            case Type.Response:
                this.handleResponsePacket(<Response>packet);
                break;

            case Type.Event:
                this.handleEventPacket(<Event>packet);
                break;

            default:
                // this.eventStream.post(new ObservableEvents.OmnisharpServerMessage(`Unknown packet type: ${packet.Type}`));
                break;
        }
    }

    private handleResponsePacket(packet: Response) 
    {
        const request = this.activeRequests[packet.requestID];

        if (request) {
            this.activeRequests[packet.requestID] = null;

            if (packet.ok) {
                request.onSuccess(<Result>packet.data);
            }
            else {
                request.onError(<Error>packet.data);
            }
        }
    }

    private handleEventPacket({ event, data }: Event) 
    {
        this.eventEmitter.emit(event, data);
    }
}
