import { ChildProcess } from 'child_process';
import { ReadLine, createInterface } from 'readline';
import { EventEmitter } from 'events';
import { Disposable } from './disposable';
import * as Protocol from './protocol';

export import EventHandler = Protocol.EventHandler;

export type ActiveRequest = Protocol.Request & { onSuccess(value: any): void; onError(error: Error): void; };

type CancellationToken = any;

export class DuplexCommunication 
{
    private requestID: number = 0;
    private readLine: ReadLine;
    private errorListener: Protocol.EventHandler;
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

    public addListener(event: string, listener: Protocol.EventHandler): Disposable {
        var eventEmitter = this.eventEmitter;

        eventEmitter.addListener(event, listener);

        var disposable: Disposable = {
            dispose() {
                eventEmitter.removeListener(event, listener)
            }
        }

        return disposable;
    }

    public send<T>(command: string, data: any, token?: CancellationToken): Promise<T> {
        var id = ++this.requestID + "";

        const packet: Protocol.Request = {
            type: Protocol.Type.Request,
            id,
            command,
            data
        };

        // (global as any).logXXXX(JSON.stringify(packet) + "\n\n");
        
        this.process.stdin.write(JSON.stringify(packet) + '\n');

        const promise = new Promise<T>((resolve, reject) => this.activeRequests[id] = {
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
        // (global as any).logXXXX(line + "\n\n");
        // if (line[0] !== '{') {
        //     this.eventStream.post(new ObservableEvents.OmnisharpServerMessage(line));
        //     return;
        // }

        let packet: Protocol.Packet;

        try {
            packet = JSON.parse(line);
        }
        catch (err) {
            // this.eventEmitter.emit("log", line);
            return;
        }

        switch (packet.type) {
            case Protocol.Type.Response:
                this.handleResponsePacket(<Protocol.Response>packet);
                break;

            case Protocol.Type.Event:
                this.handleEventPacket(<Protocol.Event>packet);
                break;

            // case Protocol.Type.Error:
            //     this.handleErrorPacket(<Protocol.Error>packet);
            
            default:
                // this.eventStream.post(new ObservableEvents.OmnisharpServerMessage(`Unknown packet type: ${packet.Type}`));
                break;
        }
    }

    private handleResponsePacket(packet: Protocol.Response) 
    {
        const request = this.activeRequests[packet.requestID];

        if (request) {
            this.activeRequests[packet.requestID] = null;

            // [dho] NOTE this flag indicates the request was delivered
            // and processed OK, NOT that the Diagnostics.Result<..> within contains no errors - 07/09/18
            if (packet.ok) {
                request.onSuccess(packet.data);
            }
            else {
                request.onError(<Error>packet.data);
            }
        }
    }

    // private handleErrorPacket(packet: Protocol.Error) 
    // {
    //     this.eventEmitter.emit('error', { description : packet.description });
    // }

    private handleEventPacket({ event, data }: Protocol.Event) 
    {
        this.eventEmitter.emit(event, data);
    }
}
