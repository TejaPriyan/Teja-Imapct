const $=s=>document.querySelector(s);
const navToggle=$('.nav-toggle');
function setNavigation(open){navToggle.setAttribute('aria-expanded',String(open));navToggle.setAttribute('aria-label',open?'Close navigation':'Open navigation');$('#navigation').classList.toggle('open',open)}
navToggle.addEventListener('click',()=>setNavigation(navToggle.getAttribute('aria-expanded')!=='true'));
$('#navigation').addEventListener('click',e=>{if(e.target.closest('a'))setNavigation(false)});
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&navToggle.getAttribute('aria-expanded')==='true'){setNavigation(false);navToggle.focus()}});
const tabs=[...document.querySelectorAll('[role=tab]')];function selectTab(tab,focus=false){for(const t of tabs){const active=t===tab;t.setAttribute('aria-selected',String(active));t.tabIndex=active?0:-1;$('#'+t.getAttribute('aria-controls')).hidden=!active}if(focus)tab.focus()}
tabs.forEach((tab,i)=>{tab.addEventListener('click',()=>selectTab(tab));tab.addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight','Home','End'].includes(e.key)){e.preventDefault();selectTab(tabs[e.key==='Home'?0:e.key==='End'?tabs.length-1:(i+(e.key==='ArrowRight'?1:-1)+tabs.length)%tabs.length],true)}})});
if(matchMedia('(pointer:coarse)').matches)selectTab($('#tab-mobile'));
// Game packages use ordinary GitHub Release links in the HTML.
// No cross-origin fetch, Blob assembly or extra phone memory is required.
