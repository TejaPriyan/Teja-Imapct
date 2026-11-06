const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const source=fs.readFileSync(path.resolve(__dirname,'../game/Assets/Plugins/WebGL/TejaBrowser.jslib'),'utf8');
function setup(){
  const library={},callbacks=[],messages=[],listeners=new Map(),timers=[],calls=[];
  let maximum=0,throwNext=false;
  const filesystem={syncFSRequests:0,syncfs(populate,callback){
    if(typeof populate==='function'){callback=populate;populate=false}
    if(throwNext){throwNext=false;throw new Error('Synchronous mount error')}
    this.syncFSRequests++;maximum=Math.max(maximum,this.syncFSRequests);calls.push(populate);
    callbacks.push(error=>{this.syncFSRequests--;callback(error)});
  }};
  const context={LibraryManager:{library},mergeInto:Object.assign,UTF8ToString:x=>x,Module:{},FS:filesystem,
    setTimeout:fn=>{timers.push(fn);return timers.length},SendMessage:(...args)=>messages.push(args),
    document:{visibilityState:'visible',addEventListener:(type,listener)=>listeners.set(type,listener),removeEventListener:(type,listener)=>{if(listeners.get(type)===listener)listeners.delete(type)}}};
  vm.runInNewContext(source,context);
  return {library,context,callbacks,messages,listeners,calls,timers,max:()=>maximum,throwNext:()=>{throwNext=true},finish:error=>{assert(callbacks.length,'Expected an in-flight operation');callbacks.shift()(error||null)}};
}

// Registration must be idempotent, even if initial population is already running.
let test=setup(),startupDone=0;
test.context.FS.syncfs(true,()=>startupDone++);
test.library.TejaRegisterBrowser('Adventure');const wrapper=test.context.FS.syncfs;
test.library.TejaRegisterBrowser('Adventure');assert.equal(test.context.FS.syncfs,wrapper);assert.equal(test.listeners.size,1);
test.context.document.visibilityState='hidden';test.listeners.get('visibilitychange')();assert.deepEqual(test.messages.pop(),['Adventure','OnTejaBrowserHidden']);
test.library.TejaSyncStorage('Adventure');assert.equal(test.calls.length,1);assert.equal(test.timers.length,1);
test.finish();assert.equal(startupDone,1);test.timers.shift()();assert.deepEqual(test.calls,[true,false]);test.finish();assert.equal(test.max(),1);

// Unity autosync, explicit populate and game saves share one FIFO without overlapping.
test=setup();test.library.TejaRegisterBrowser('Adventure');const completed=[];
test.context.FS.syncfs(error=>completed.push(['unity',error]));
test.library.TejaSyncStorage('Adventure');test.library.TejaSyncStorage('Adventure');test.library.TejaSyncStorage('Adventure');
test.context.FS.syncfs(true,error=>completed.push(['populate',error]));
assert.deepEqual(test.calls,[false]);test.finish();assert.deepEqual(completed,[['unity',null]]);
test.finish();assert.deepEqual(test.calls,[false,false,true]);
test.finish();assert.deepEqual(completed,[['unity',null],['populate',null]]);
assert.deepEqual(test.calls,[false,false,true,false]);test.finish();assert.equal(test.context.Module.tejaSyncRunning,false);assert.equal(test.max(),1);

// Errors preserve the error argument and never discard a newer queued game save.
test=setup();test.library.TejaRegisterBrowser('Adventure');const failure=new Error('IDB unavailable'),results=[];
test.library.TejaSyncStorage('Adventure');test.library.TejaSyncStorage('Adventure');
test.context.FS.syncfs(true,error=>results.push(error));test.context.FS.syncfs(false,error=>results.push(error));
test.finish(failure);assert.deepEqual(test.messages.pop(),['Adventure','OnTejaBrowserStorageError','unavailable']);
test.finish(failure);assert.equal(results[0],failure);test.finish();test.finish();
assert.deepEqual(test.calls,[false,true,false,false]);assert.equal(results[1],null);assert.equal(test.max(),1);assert.equal(test.context.Module.tejaSyncRunning,false);
test.library.TejaSyncStorage('Adventure');test.finish();assert.equal(test.callbacks.length,0);

// Synchronous native failures and callbacks that throw must release the queue too.
test=setup();test.library.TejaRegisterBrowser('Adventure');test.throwNext();let syncError;
test.context.FS.syncfs(false,error=>{syncError=error});assert.match(syncError.message,/mount error/);
test.context.FS.syncfs(false,()=>{throw new Error('Caller failure')});let later=false;
test.context.FS.syncfs(false,()=>{later=true});assert.throws(()=>test.finish(),/Caller failure/);test.finish();assert(later);assert.equal(test.max(),1);
console.log('PASS: startup populate guard; single shared FS.syncfs queue; external autosync and populate callbacks preserved; coalesced game saves survive errors; callback/throw recovery; visibility handler registered once.');

