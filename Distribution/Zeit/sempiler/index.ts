
import { DuplexCommunication } from './process';
import * as Diagnostics from './diagnostics';
import * as Launcher from './launcher';
import { ChildProcess } from 'child_process';

export { Diagnostics };

export interface Config
{

}

export interface SourceConfig
{
    path : string,
    dirs? : SourceDirectory[],
    files? : SourceFile[],
    literals? : SourceLiteral[]
}

export interface SourceDirectory
{ path : string }

export interface SourceFile
{ path : string }

export interface SourceLiteral
{ path? : string, text : string }

type CancellationToken = any;


export interface Session 
{
	start : string;
    end : string;
    baseDirectory : string;
	artifacts : { [name : string] : Artifact };
	filesWritten : { [name : string] : string[] };
}

export interface Artifact
{
	name: string;
	role : 'client' | 'server';
	targetLang : string;
	targetPlatform : string;
}


export class Sempiler
{
    constructor(private duplexComms : DuplexCommunication)
    {
    }

    public dispose()
    {
        this.duplexComms.dispose();
        this.duplexComms.process.kill();
    }

    // public setConfig(config : Config, token? : CancellationToken)
    // {
    //     return this.duplexComms.send<Diagnostics.Result<void>>('set_config', config, token);
    // }
  
    public setMain(path : string, token? : CancellationToken)
    {
        return this.duplexComms.send<Diagnostics.Result<void>>('set_main', { path }, token);
    }

    public setOverrideSources(source : SourceConfig, token? : CancellationToken)
    {
        return this.duplexComms.send<Diagnostics.Result<void>>('set_override_sources', source, token);
    }

    public compile(/*source : SourceConfig,*/ token? : CancellationToken)
    {
        return this.duplexComms.send<Diagnostics.Result<Session>>('compile', {}, token);
    }

    public onError(callback : (data) => void)
    {
        this.duplexComms.addListener("error", callback);
    }

    // public onLog(callback : (data) => void)
    // {
    //     this.duplexComms.addListener("log", callback);
    // }

    // public onCompilationResult(handler : EventHandler)
    // {
    //     return this.duplexComms.addListener('compilation', handler);
    // }
}


export async function createSempiler(cwd : string, absMainPath : string) : Promise<Diagnostics.Result<Sempiler>>
{
    const result : Diagnostics.Result<Sempiler> = {};

    // [dho] install - TODO, check is installed or else download it etc. - 01/09/18

    let semProcess : ChildProcess = null;

    // launch
    try
    {
        const args : string[] = [];

        // configPath && args.push('--config', configPath);
        console.log("Running launcher");
        
        const launchResult = await Launcher.launch(process.env.SEMPILER_PATH, cwd, args);
        
        semProcess = launchResult.process;
   
        const duplexComms = new DuplexCommunication(semProcess);
        
        const sempiler = new Sempiler(duplexComms);
        
        // [dho] TODO CLEANUP HACK! - 02/06/19
        
        const t = setTimeout(() => 
            console.log(
`HACK! If the process is hanging at this message, it is because it was not killed properly last time and is running somewhere. 

Open activity monitor and kill it, then refresh the dashboard and try again!`)
        , 5000);
        
        Diagnostics.addMessages(result, (await sempiler.setMain(absMainPath)).messages);
        
        clearTimeout(t);

        result.value = sempiler;
    }
    catch(err)
    {
        Diagnostics.addError(result, Diagnostics.createErrorFromException(err));

        return result;
    }
    


    return result;
}