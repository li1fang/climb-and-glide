import fs from 'node:fs';
import path from 'node:path';
const [trial, playerLog] = process.argv.slice(2);
if (!trial || !playerLog) throw Error('Usage: node scripts/analyze-a2.mjs <capture-directory> <player.log>');
const rows = fs.readFileSync(path.join(trial,'frames.csv'),'utf8').trim().split(/\r?\n/).slice(1).map(x=>x.split(','));
const phases = [...new Set(rows.map(x=>x[2]))];
const dt = rows.map(x=>Number(x[1])*1000).filter(Number.isFinite).sort((a,b)=>a-b);
const logs=fs.readFileSync(playerLog,'utf8');
const errors=logs.split(/\r?\n/).filter(x=>/Exception:|Error:|A2 timeout|Shader error/i.test(x));
const screenshots=['Climbing','PreparingFlight','Flying','FlightMid','Approach','Landed'].map(x=>({phase:x,exists:fs.existsSync(path.join(trial,x+'.png'))}));
const completion=JSON.parse(fs.readFileSync(path.join(trial,'completed.json'),'utf8'));
const flight=rows.filter(x=>x[2]==='Flying');
const flightDt=flight.map(x=>Number(x[1])*1000).sort((a,b)=>a-b);
const flightSeconds=Number(rows.find(x=>x[2]==='Landed')?.[0])-Number(flight[0]?.[0]);
const finite=rows.every(x=>x.slice(0,2).concat(x.slice(3)).every(y=>Number.isFinite(Number(y))));
const minimumAgl=Math.min(...rows.map(x=>Number(x[7])));
const initialFlightAltitude=Number(flight[0]?.[6]);
const bearingsValid=rows.every(x=>Math.abs(Number(x[9]))<=180.01);
const result={flightFrameMs:{median:flightDt[Math.floor(flightDt.length*.5)],p95:flightDt[Math.floor(flightDt.length*.95)],max:flightDt.at(-1)},flightSeconds,finite,minimumAgl,initialFlightAltitude,bearingsValid,landingTargetDistance:Number(rows.at(-1)[8]),scope:'Rendered Windows player, synthetic input; VR and PUMP excluded',frames:rows.length,
 seconds:Number(rows.at(-1)[0]),phases,frameMs:{median:dt[Math.floor(dt.length*.5)],p95:dt[Math.floor(dt.length*.95)],max:dt.at(-1)},
 screenshots,errors,passed:completion.landed && finite && minimumAgl>=-.02 && initialFlightAltitude>91 && bearingsValid && flightSeconds>40 && phases.join(',')==='Climbing,PreparingFlight,Flying,Landed' && screenshots.every(x=>x.exists) && errors.length===0 && rows.length>100};
fs.writeFileSync(path.join(trial,'analysis.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
if(!result.passed) process.exitCode=1;
