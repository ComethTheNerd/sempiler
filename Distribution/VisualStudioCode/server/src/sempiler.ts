
import { CancellationToken, Disposable } from 'vscode-languageserver'
import { DuplexCommunication } from './process';
import * as Diagnostics from './diagnostics';
import * as Launcher from './launcher';
import { ChildProcess } from 'child_process';

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

export class Sempiler implements Disposable
{
    constructor(private duplexComms : DuplexCommunication)
    {
    }

    public dispose()
    {
        this.duplexComms.dispose();
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
        return this.duplexComms.send<Diagnostics.Result<void>>('compile', {}, token);
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


export async function createSempiler(cwd : string, mainAbsPath : string) : Promise<Diagnostics.Result<Sempiler>>
{
    const result : Diagnostics.Result<Sempiler> = {};

    // [dho] install - TODO, check is installed or else download it etc. - 01/09/18

    let process : ChildProcess = null;

    // launch
    try
    {
        const args : string[] = [];

        // configPath && args.push('--config', configPath);

        // [dho] TODO change to *real* path! - 07/09/18
        const launchResult = await Launcher.launch('/Users/QuantumCommune/Documents/Projects/Sempiler/Distribution/MockSempilerInstallation/CLI', cwd, args);
    
        process = launchResult.process;
    }
    catch(err)
    {
        Diagnostics.addError(result, Diagnostics.createErrorFromException(err));
        
        return result;
    }
    

    
    const duplexComms = new DuplexCommunication(process);
    
    const sempiler = new Sempiler(duplexComms);
    
    Diagnostics.addMessages(result, (await sempiler.setMain(mainAbsPath)).messages);
    
    result.value = sempiler;

    return result;
}