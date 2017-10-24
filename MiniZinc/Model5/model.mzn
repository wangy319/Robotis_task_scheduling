include "cumulative.mzn";
include "member.mzn";
include "alldifferent.mzn";
include "circuit.mzn";
include "inverse.mzn";
include "disjoint.mzn";
include "diff2.mzn";
include "element.mzn";
include "count.mzn";
include "global_cardinality.mzn";

int:nbrTasks;
int:nbrMachines;
int:nbrTools;
int:nbrFixtures;
int:nbrComponents;
int:nbrCycles;
int:nbrUniqueTasks = nbrTasks div nbrCycles;
set of int: tasks = 1..nbrTasks;
set of int: machines = 1..nbrMachines;
set of int: tools = 1..nbrTools;

set of int: startTasks = nbrTasks+1..nbrTasks+nbrMachines;
set of int: goalTasks = nbrTasks+nbrMachines+1..nbrTasks+nbrMachines*2;

array[1..nbrTasks+nbrMachines*2] of string:name;
array[1..nbrTasks+nbrMachines*2] of int:duration;
array[1..nbrUniqueTasks] of set of int:identicalTasks;
array[tasks] of int:cycleNbr;
array[tasks] of int:tray;
array[tasks] of int:out_put;
array[tasks] of int:fixture;
array[tasks] of set of int:componentsUsed;

array[tasks] of int:componentCreated;

set of int:putting;
set of int:mounting;
set of int:taking;
set of int:moving;

int:nbrConcurrentGroups;
array[1..nbrConcurrentGroups] of set of int:concurrentTasks;

int:orderedGroups;
int:maxOrderedLength;
array[1..orderedGroups,1..maxOrderedLength] of int:order;

array[1..nbrTasks+nbrMachines] of int: toolNeeded; % The tool nedded for each task. 0 means no specific tool is required

array[tools,tools] of int: changeToolDuration;

set of int:fixtures = {fixture[i]|i in tasks where fixture[i] > 0}; % The set of fixtures available
set of int:trays = {tray[i]|i in tasks where tray[i] > 0}; % The set of trays available
set of int:components = 1..nbrComponents;
set of int:cycles = {cycleNbr[i]|i in tasks}; % The set of cycles used
array[components] of set of int:putting_c = [{task|task in putting where c in componentsUsed[task]}|c in components];
array[components] of set of int:mounting_c = [{task|task in mounting where c in componentsUsed[task]}|c in components];
array[components] of set of int:taking_c = [{task|task in taking where c in componentsUsed[task]}|c in components];
array[components] of set of int:moving_c = [{task|task in moving where c in componentsUsed[task]}|c in components];
array[components] of set of int:actions_c = [putting_c[c] union mounting_c[c] union taking_c[c] union moving_c[c]|c in components];

set of int: orderedSet = {order[g,t]|g in 1..orderedGroups, t in 1..maxOrderedLength where order[g,t] != 0};

array[1..nbrTasks+nbrMachines*2] of var machines: usingMachine; % The machine each task uses
array[1..nbrTasks+nbrMachines,1..nbrTasks+nbrMachines] of int:timeToTask; % The time it takes to move from one task to another
array[1..nbrTasks+nbrMachines] of int:timeToChange;
array[1..nbrTasks+nbrMachines*2] of int:timeFromChange;
array[1..nbrTasks+nbrMachines*2] of var 1..nbrTasks+nbrMachines*2:pred; % The predecessor task for each task. The first tasks has predecessor 0
% array[tasks] of var 0..1:hasToolChangeBefore;

% array[1..nbrFixtures,tasks] of var 0..nbrTasks:fixtureOccupation;

% int: maxToolChangeDur = max([changeToolDuration[i,j]|i,j in tools]);
% int: maxToolChangeMoveDur = max([timeToChange[from] + timeFromChange[to]|from in 1..nbrTasks+nbrMachines, to in tasks]) + maxToolChangeDur;
% int: maxMoveDur = max(max([timeToTask[from,to]|from in 1..nbrTasks+nbrMachines,to in 1..nbrTasks+nbrMachines]),maxToolChangeMoveDur);
array[tasks] of int: maxMoveDurs = [max([timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j])|task in tasks];
array[tasks] of int: minMoveDurs = [min([timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j])|task in tasks];
int: maxEnd = sum(duration)+sum(maxMoveDurs);
int: minEnd = (sum(duration)+sum(minMoveDurs)) div nbrMachines;

int: total = sum(i in tasks)(duration[i]);
array [1..nbrTasks+nbrMachines*2] of var 0..maxEnd: start;
array [1..nbrTasks+nbrMachines*2] of var 0..maxEnd: end;
var minEnd..maxEnd: total_end;

array[tasks] of var 0..maxEnd:moveStart;
array[tasks] of var 0..max(maxMoveDurs):moveDuration;
array[tasks] of var 0..maxEnd:moveEnd;

array[1..nbrTasks+nbrMachines] of var 1..nbrTools:toolUsed;

% array[1..nbrTasks+nbrMachines*2] of var 1..nbrTasks+nbrMachines*2:succ;

array[tasks] of set of int:taskSubComponents; % Only the components one subassembly down
array[tasks] of set of int:taskCompleteSubComponents; % All the way down to the initial components
array[components] of set of int:subComponents; % Only the components one subassembly down

int:timeMatrix3DDepth;
array[1..nbrTasks+nbrMachines,tasks,1..timeMatrix3DDepth] of int: timeMatrix3D;

%-----------------------------------------------------------------------------------%
%Constraints

constraint
  forall(task in tasks)(
    trace(show(task)++":"++show(name[task])++"\n",
    true
)
  );

% All ends has to be lesser than the total end
constraint
  forall(task in tasks)(
    end[task] <= total_end
  );

% End times
constraint
  forall(i in tasks)(
    end[i] = start[i] + duration[i]
  );

constraint
  forall(i in tasks)(
    moveEnd[i] = moveStart[i] + moveDuration[i]
  );

% Start tasks start at time 0
constraint
  forall(startTask in startTasks)(
    start[startTask] = 0
  );

% Goal tasks start at time 0
constraint
  forall(goalTask in goalTasks)(
    start[goalTask] = 0
  );

% Each start task gets assigned to a machine
constraint
  forall(machine in machines)(
    usingMachine[nbrTasks+machine] = machine
  );

% Limit the duration domains
constraint
  forall(task in tasks)(
    let{
      set of int:nonValidDurs =  0..max(maxMoveDurs) diff {timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j}
    }in
      forall(i in nonValidDurs)(
        moveDuration[task] != i
      )
  );


% Precedences

% The assembly has to start with a take task
constraint
  let{
    int:minTakeDur = min([duration[task]|task in taking])
  }in
  forall(task in tasks where not(task in taking))(
    moveStart[task] >= minTakeDur
  );

% If a set of tasks on a component involves a mount task and a put task, the put task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(put_task in putting_c[comp])(
        end[put_task] <= moveStart[mount_task] /\ pred[put_task] != mount_task
      )
    )
  );

% If a set of tasks on a component involves a mount task and a take task, the take task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
        end[take_task] <= moveStart[mount_task] /\ pred[take_task] != mount_task
      )
    )
  );

% If a component has a put and take performed on it in a tray, the put has to come before the take
constraint
  forall(comp in components)(
    forall(putTask in putting_c[comp] where tray[putTask] > 0)(
      forall(takeTask in taking_c[comp] where tray[putTask] = tray[takeTask])(
        end[putTask] <= moveStart[takeTask] /\ pred[putTask] != takeTask
      )
    )
  );




% For every put action on a fixture, there is a take action. The put action has to come before the take action
constraint
  forall(f in fixtures)(
    forall(putTask in putting where fixture[putTask] = f)(
      forall(takeTask in taking where fixture[takeTask] = f /\ componentsUsed[putTask] subset taskSubComponents[takeTask])(
        end[putTask] <= moveStart[takeTask] /\ pred[putTask] != takeTask
      )
    )
  );

% Limitation: This constraint only allows two parts to be mounted before it has to be taken from the fixture. For example, it cannot handle the scenario of mounting two switches into the bottom component before taking the assembled component from the fixture. This is due to the composition of the subcomponent array.
% The intervals between when components are put and then taken again cannot overlap on the same fixture
constraint
  forall(f in fixtures)(
      let{
        int: nbrPuts = length([task|task in putting where fixture[task] = f]),
        array[1..nbrPuts] of int: puts = [task|task in putting where fixture[task] = f],
        array[1..nbrPuts] of int: takes = [take|p in 1..nbrPuts, take in taking where fixture[take] = f /\ componentsUsed[puts[p]] subset taskCompleteSubComponents[take]]
      } in
        cumulative([moveStart[task]|task in puts], [abs(end[takes[i]]-moveStart[puts[i]])|i in 1..nbrPuts], [1|i in 1..nbrPuts], 1)
  );


% Concurrent tasks
constraint
  forall(group in 1..nbrConcurrentGroups)(
    forall(task1 in concurrentTasks[group])(
      forall(task2 in concurrentTasks[group] diff {task1})(
        start[task1] = start[task2] /\ usingMachine[task1] != usingMachine[task2] /\ pred[task1] != task2 /\ pred[task2] != task1
      )
    )
  );

% Components cannot be used before they are created
constraint
  forall(task1 in tasks where componentCreated[task1] > 0)(
    forall(task2 in tasks where componentCreated[task1] in componentsUsed[task2])(
      moveStart[task2] >= end[task1] /\ pred[task1] != task2
    )
  );

% Task using a component cannot execute before all the tasks having it as subcomponent
constraint
  forall(precTask in tasks)(
    forall(task in tasks where precTask != task /\ componentsUsed[precTask] union taskCompleteSubComponents[task] != {} /\ componentsUsed[precTask] union taskCompleteSubComponents[task] subset taskCompleteSubComponents[task])(
      end[precTask] <= moveStart[task] /\ pred[precTask] != task
    )
  );

% Limit the domains
% Lower bound
constraint
  forall(t in tasks where componentCreated[t] > 0)(
    let{
      int: nbrPrevTasks = length([task|task in tasks where componentCreated[task] in componentsUsed[t]]),
      array[1..nbrPrevTasks] of int: prevTasks = [duration[task]+min([timeMatrix3D[task,j,k]|j in tasks,k in 1..timeMatrix3DDepth where j != task])|task in tasks where componentCreated[task] in componentsUsed[t]]
    }in
    if nbrPrevTasks > 0 then
      if nbrMachines >= nbrPrevTasks then
        % if the number of machines are greater than the number of task preceeding task t, then the best scheduling is to do all tasks in parallell. If so the earliest task t can start is equal to the maximum of the earlier tasks
        start[t] >= max(prevTasks)
      else
        % If the number of machines are less than the number of tasks preceeding task t, then the best we can do is to divide the task times equally on all machines. If the tasks can be divided onto the machines so that the total length of the times on all machines are the same, that time will be equal to sum(prevTasks)/nbrMachines. If they don't match up the maximum of these times will be larger than sum(prevTasks)/nbrMachines.
        start[t] >= sum(prevTasks) div nbrMachines
      endif
    else
      true
    endif
  );
% Upper bound of task using components later used as subcomponents
constraint
  forall(precTask in tasks)(
    let{
      int: nbrSuccTasks = length([task|task in tasks where componentsUsed[precTask] union taskCompleteSubComponents[task] subset taskCompleteSubComponents[task] /\ componentsUsed[precTask] union taskCompleteSubComponents[task] != {}]),
      array[1..nbrSuccTasks] of int: succTasks = [duration[task]+min([timeMatrix3D[task,j,k]|j in tasks,k in 1..timeMatrix3DDepth where j != task])|task in tasks where componentsUsed[precTask] union taskCompleteSubComponents[task] subset taskCompleteSubComponents[task] /\ componentsUsed[precTask] union taskCompleteSubComponents[task] != {}]
    }in
    if nbrSuccTasks > 0 then
      if nbrMachines >= nbrSuccTasks then
        % if the number of machines are greater than the number of task succeeding task precTask, then the best scheduling is to do all tasks in parallell. If so the latest task precTask can end is equal to maxEnd minus the maximum of the earlier tasks
        end[precTask] <= maxEnd - max(succTasks)
      else
        % If the number of machines are less than the number of tasks succeeding task precTask, then the best we can do is to divide the task times equally on all machines. If the tasks can be divided onto the machines so that the total length of the times on all machines are the same, that time will be equal to sum(succTasks)/nbrMachines. If they don't match up the maximum of these times will be larger than sum(succTasks)/nbrMachines.
        end[precTask] <= maxEnd - sum(succTasks) div nbrMachines
      endif
    else
      true
    endif
  );


% Tasks on the same fixture can't overlap
constraint
  forall(f in fixtures)(
    let{
      int:nbrFixtureTasks = length([task|task in tasks where fixture[task] = f]),
      array[1..nbrFixtureTasks] of int:fixtureTasks = [task|task in tasks where fixture[task] = f]
    }in
    if length(fixtureTasks) > 1 then
      cumulative([start[task]|task in fixtureTasks],[duration[task]|task in fixtureTasks],[1|task in fixtureTasks],1)   
    else
      true
    endif
  );

% Tasks on the same tray can't overlap
constraint
  forall(t in trays)(
    let{
      int:nbrTrayTasks = length([task|task in tasks where tray[task] = t]),
      array[1..nbrTrayTasks] of int:trayTasks = [task|task in tasks where tray[task] = t]
    }in
    if length(trayTasks) > 1 then
      cumulative([start[task]|task in trayTasks],[duration[task]|task in trayTasks],[1|task in trayTasks],1)
    else
      true
    endif
  );

% There can only be as many tasks performed simultaneously as there are machines
%  constraint
%    cumulative(start, duration, [1|t in 1..nbrTasks+nbrMachines*2], nbrMachines);

% % Predecessors

% % The start tasks predecessors are the goal tasks
% constraint  
%   forall(startTask in startTasks)(
%     pred[startTask] = startTask+nbrMachines
%   );


% % A task cannot be a predecessor to itself
% constraint
%   forall(task in 1..nbrTasks+nbrMachines*2)(
%     pred[task] != task
%   );

% A task can only start after the move to it
constraint
  forall(task in tasks)(
    start[task] >= moveEnd[task]
  );

% A task has to start after its predecessor
constraint
   forall(task in tasks)(
      moveStart[task] >= end[pred[task]]
  );


% A task can only be the predecessor to 1 task
constraint
  alldifferent(pred)::domain;

% For all start tasks except the first one
constraint  
  forall(startTask in nbrTasks+2..nbrTasks+nbrMachines)(
    % The start tasks predecessor are the previous goal task
    pred[startTask] = startTask+nbrMachines-1
  );

% The first start tasks predecessor is the last goal task
constraint
  let{
    int: firstStartTask = nbrTasks+1,
    int: lastGoalTask = nbrTasks+nbrMachines*2
  }in
  pred[firstStartTask] = lastGoalTask;


constraint
  circuit(pred);

% % A take task cannot be a predecessor to any task but put tasks, mount taks and move tasks
% % Needed? Restricts pred for "Grab top-button" in a good way, but restricts "Lift, support" in a bad
% constraint
%   forall(takeTask in taking where not(takeTask in {order[i,j]|i in 1..orderedGroups, j in 1..maxOrderedLength}))(
%     forall(task in tasks diff (mounting union putting union moving))(
%       pred[task] != takeTask
%     )
%   );

% If a set of tasks on a component involves a mount and a take task, but no move tasks or put task on the same fixture, tray or output as the mount, the take task is the predecessor of the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      let{
        set of int: puts={p|p in putting_c[comp] where (fixture[p] > 0 /\ fixture[p] = fixture[mount_task]) \/ (out_put[p] > 0 /\ out_put[p] = out_put[mount_task]) \/ (tray[p] > 0 /\ tray[p] = tray[mount_task])}
      } in
      forall(take_task in taking_c[comp] where not(take_task in orderedSet) /\ puts = {})(
          pred[mount_task] = take_task
      )
    )
  );


% If a set of tasks on a component involves a put task not in a tray and a take task, the take task ha to be the predecessor of the put task
constraint
  forall(comp in components)(
    forall(put_task in putting_c[comp] where tray[put_task] = 0)(
      forall(take_task in taking_c[comp])(
        pred[put_task] = take_task
      )
    )
  );

% Take tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  forall(takeTask1,takeTask2 in taking where takeTask1 != takeTask2)(
    pred[takeTask1] != takeTask2
  );

% Mount tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  forall(mountTask1,mountTask2 in mounting where mountTask1 != mountTask2)(
    pred[mountTask1] != mountTask2
  );

% Put tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  forall(putTask1,putTask2 in putting where putTask1 != putTask2)(
    pred[putTask1] != putTask2
  );

% A task1 cannot have task2 as predecessor if task2 uses a component that has the component tak1 uses as a subcomponent
constraint
  forall(task in tasks)(
    let{
      set of int: nonPredecessors = {t|t in tasks where task != t /\ (componentsUsed[task] subset taskCompleteSubComponents[t] \/ componentsUsed[task] subset subComponents[componentCreated[t]])}
    }in
      forall(nonPred in nonPredecessors)(
        pred[task] != nonPred
      )
  );

% Order
constraint
  forall(group in 1..orderedGroups)(
    forall(t in 1..maxOrderedLength-1 where order[group,t+1] != 0)(
      let{
        int:task1 = order[group,t],
        int:task2 = order[group,t+1],
      } in
       pred[task2] = task1
       /\
      % Filter pred domains
      forall(task in tasks union goalTasks where task != task2)(
        pred[task] != task1
      )
    )
  );


% % The tasks that has to be performed in the interval between when the component is put and then taken again cannot have as predecessors other tasks in a similar interval for other put and take tasks on the same fixture
% constraint
%   forall(f in fixtures)(
%       let{
%         int: nbrPuts = length([task|task in putting where fixture[task] = f]),
%         array[1..nbrPuts] of int: puts = [task|task in putting where fixture[task] = f],
%         array[1..nbrPuts] of int: takes = [take|p in 1..nbrPuts, take in taking where fixture[take] = f /\ componentsUsed[puts[p]] subset taskCompleteSubComponents[take]]
%       } in
%       forall(pair in 1..nbrPuts-1)(
%         let{
%           set of int:fixTasksPair1 = {task|task in tasks where fixture[task] = fixture[takes[pair]] /\ componentsUsed[task] subset (taskCompleteSubComponents[takes[pair]] union componentsUsed[takes[pair]]) /\ puts[pair] != task},
%           set of int:fixTasksPair2 = {task|task in tasks where fixture[task] = fixture[takes[pair+1]] /\ componentsUsed[task] subset (taskCompleteSubComponents[takes[pair+1]] union componentsUsed[takes[pair+1]]) /\ puts[pair+1] != task}
%         }in
%         forall(task1 in fixTasksPair1)(
%           forall(task2 in fixTasksPair2)(
%             pred[task1] != task2 /\ pred[task2] != task1
%           )
%         )
%         % The tasks inside an interval including the take task cannot be predecessors or successors to the corresponding tasks in another interval
%         /\
%         forall(task1 in fixTasksPair1)(
%           pred[task1] != puts[pair+1]
%         )
%         /\
%         forall(task2 in fixTasksPair2)(
%           pred[task2] != puts[pair]
%         )
%         /\
%         % The tasks in an interval inculding the take task cannot be the first tasks in an assembly
%         forall(task1 in fixTasksPair1)(
%           forall(startTask in startTasks)(
%             pred[task1] != startTask
%           )
%         )
%         /\
%         forall(task2 in fixTasksPair2)(
%           forall(startTask in startTasks)(
%             pred[task2] != startTask
%           )
%         )
%         /\
%         % Tasks in an interval including the put task cannot be the last tasks in an assembly
%         forall(task1 in (fixTasksPair1 union {puts[pair]}) diff {takes[pair]})(
%           forall(goalTask in goalTasks)(
%             pred[goalTask] != task1
%           )
%         )
%         /\
%         forall(task2 in (fixTasksPair2 union {puts[pair]}) diff {takes[pair+1]})(
%           forall(goalTask in goalTasks)(
%             pred[goalTask] != task2
%           )
%         )
%     )
%   );

% If there is a set of concurrent tasks on a subset of tasks using as many machines as available, the tasks after the concurrent tasks cannot have the tasks before the concurrent tasks as predecessors
constraint
  forall(concGroup in concurrentTasks)(
    if card(concGroup) = nbrMachines then
      let{
        set of int: concComps = array_union([componentsUsed[i]|i in concGroup]),
        set of int: concSubComps = array_union([taskCompleteSubComponents[i]|i in concGroup]),
        set of int: postTasks = {postTask|postTask in tasks where concComps intersect taskCompleteSubComponents[postTask] != {}},
        set of int: preTasks = {preTask|preTask in tasks where componentsUsed[preTask] intersect concSubComps != {}}
      }in
      forall(postTask in postTasks)(
        forall(preTask in preTasks)(
          pred[postTask] != preTask
        )
      )
    else
      true
    endif
  );

% Put tasks cannot be first in a circuit
constraint
  forall(putTask in putting)(
    forall(startTask in startTasks)(
      pred[putTask] != startTask
    )
  );

% Mount tasks cannot be first in a circuit
constraint
  forall(mountTask in mounting)(
    forall(startTask in startTasks)(
      pred[mountTask] != startTask
    )
  );

% Take tasks cannot be last in a circuit
constraint
  forall(takeTask in taking)(
    forall(goalTask in goalTasks)(
      pred[goalTask] != takeTask
    )
  );

% One of the output tasks has to be last on one of the circuits
constraint
  let{
    int: nbrGoalPreds = length([pred[task]|task in goalTasks]),
    array[1..nbrGoalPreds] of var int:goalPreds = [pred[task]|task in goalTasks],
    int: nbrOutputTasks = length([task|task in tasks where out_put[task] > 0]),
    array[1..nbrOutputTasks] of int: outputTasks = [task|task in tasks where out_put[task] > 0],
    array[1..nbrOutputTasks] of var 0..1: counts
  }in
    global_cardinality(goalPreds, outputTasks, counts)
    /\
    sum(counts) > 0;

% One of the take tasks, thats not on an output, has to be first on one of the circuits
constraint
  let{
    int: nbrTakePreds = length([pred[task]|task in taking where out_put[task] = 0]),
    array[1..nbrTakePreds] of var int:takePreds = [pred[task]|task in taking where out_put[task] = 0],
    int: nbrStartTasks = card(startTasks),
    array[1..nbrStartTasks] of int: startTasksArray = [task|task in startTasks],
    array[1..nbrStartTasks] of var 0..1: counts
  }in
    global_cardinality(takePreds, startTasksArray, counts)
    /\
    sum(counts) > 0;

% A task has to use the same machine as its predecessor
constraint
  forall(task in tasks union goalTasks)(
    usingMachine[task] = usingMachine[pred[task]]
  );

% All other tasks has to have the same tool as its predecessor or do a change first
constraint
  forall(task in tasks)(
    moveDuration[task] = timeMatrix3D[pred[task],task,abs(toolUsed[task]-toolUsed[pred[task]])+1]
  );


constraint
  forall(task in 1..nbrTasks+nbrMachines where toolNeeded[task] != 0)(
    toolUsed[task] = toolNeeded[task]
  );

% % Cycle order
%% constraint
%%   forall(it in identicalTasks)(
%%     forall(task1 in it)(
%%       forall(task2 in it where cycleNbr[task1] < cycleNbr[task2])(
%%           task1EndBeforeTask2Start(task1, task2)
%%       )
%%     )
%%   );

solve :: seq_search([
                      int_search([pred[task]|task in taking where out_put[task] = 0], first_fail, indomain_max, complete),
                      int_search([pred[task]|task in putting where out_put[task] = 0], first_fail, indomain_median, complete),
                      int_search([pred[task]|task in mounting where out_put[task] = 0], first_fail, indomain_median, complete),
                      int_search([pred[task]|task in tasks where out_put[task] != 0], first_fail, indomain_median, complete),
                      int_search(pred, first_fail, indomain_median, complete),
%                       int_search(toolUsed, first_fail, indomain_random, complete),
                      int_search(start, smallest, indomain_min, complete)
])
        minimize total_end;
%% solve minimize total_end;

output ["Minimizing = " ++ show(total_end) ++ "\n"]++["End = " ++ show(max(end)) ++ "\n"] ++ 
["start duration i usingMachine pred name hasToolChangeBefore\n"] ++
[show(moveStart[i]) ++ " " ++ show(moveDuration[i]) ++ " 0 0 0 0" ++ " Move from " ++ show(pred[i]) ++ " to " ++ show(i) ++ "\n" ++
show(start[i]) ++ " " ++ show(duration[i]) ++ " " ++ show(i) ++ " " ++ show(usingMachine[i]) ++ " " ++ show(pred[i])++
%  " " ++ show(succ[i]) ++ 
" " ++ show(name[i]) ++ " TU" ++ show(toolUsed[i]) ++
% " succ-"++show(succ[i])++
" pred-"++show(pred[i])++ "\n"|i in tasks]++["#"++show(start[i]) ++ " " ++ show(duration[i]) ++ " " ++ show(i) ++ " " ++ show(usingMachine[i]) ++ " " ++ show(pred[i]) ++ " " ++ show(name[i])++
% " succ-"++show(succ[i])++
" pred-"++show(pred[i])++"\n"|i in nbrTasks+1..nbrTasks+nbrMachines]++["\nGoal tasks:\n"]++["#"++show(start[i]) ++ " " ++ show(duration[i]) ++ " " ++ show(i) ++ " " ++ show(usingMachine[i]) ++ " " ++ show(pred[i]) ++ " " ++ show(name[i])++
% " succ-"++show(succ[i])++
" pred-"++show(pred[i])++"\n"|i in nbrTasks+nbrMachines+1..nbrTasks+nbrMachines*2]++["\n"]++[show(pred)];

