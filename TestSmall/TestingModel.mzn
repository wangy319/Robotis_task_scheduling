include "cumulative.mzn";
include "member.mzn";
include "alldifferent.mzn";
include "circuit.mzn";
include "inverse.mzn";
include "disjoint.mzn";
include "element.mzn";
include "count.mzn";
include "global_cardinality.mzn";

int:nbrTasks;
int:nbrMachines;
int:nbrTools;
int:nbrFixtures;
int:nbrComponents;
int:nbrTrays;
int:nbrOutputs;
set of int: tasks = 1..nbrTasks;
set of int: machines = 1..nbrMachines;
set of int: tools = 1..nbrTools;
set of int:components = 1..nbrComponents;
set of int:fixtures = 1..nbrFixtures;
set of int:trays = 1..nbrTrays;
set of int:outputs = 1..nbrOutputs;

set of int: startTasks = nbrTasks+1..nbrTasks+nbrMachines;
set of int: goalTasks = nbrTasks+nbrMachines+1..nbrTasks+nbrMachines*2;
set of int: allTasks = 1..nbrTasks+nbrMachines*2;

array[1..nbrTasks+nbrMachines*2] of string:name;
array[1..nbrTasks+nbrMachines*2] of int:duration;
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

array[components] of set of int:putting_c = [{task|task in putting where c in componentsUsed[task]}|c in components];
array[components] of set of int:mounting_c = [{task|task in mounting where c in componentsUsed[task]}|c in components];
array[components] of set of int:taking_c = [{task|task in taking where c in componentsUsed[task]}|c in components];
array[components] of set of int:moving_c = [{task|task in moving where c in componentsUsed[task]}|c in components];

set of int: orderedSet = {order[g,t]|g in 1..orderedGroups, t in 1..maxOrderedLength where order[g,t] != 0};

array[1..nbrTasks+nbrMachines*2] of var machines: usingMachine; % The machine each task uses

array[1..nbrTasks+nbrMachines*2] of var 1..nbrTasks+nbrMachines*2:pred; % The predecessor task for each task. The first tasks has predecessor 0

int:timeMatrix3DDepth;
array[1..nbrTasks+nbrMachines,tasks,1..timeMatrix3DDepth] of int: timeMatrix3D;

% Used since int includes negative integers as well
int:maxE = (max(duration)+max([timeMatrix3D[i,j,k]|i in tasks, j in tasks, k in 1..timeMatrix3DDepth]))*nbrTasks;%can maxE be made smaller to make search faster?
array [1..nbrTasks+nbrMachines*2] of var 0..maxE: start;
array [1..nbrTasks+nbrMachines*2] of var 0..maxE: end;
var 0..maxE: makespan;

bool: tempFilter;
bool: predFilter;

array[tasks] of var 0..maxE:moveStart;
array[tasks] of var 0..maxE:moveDuration;
array[tasks] of var 0..maxE:moveEnd;




array[1..nbrTasks+nbrMachines*2] of var tools:toolUsed;

array[tasks] of set of int:taskSubComponents; % Only the components one subassembly down
array[tasks] of set of int:taskCompleteSubComponents; % All the way down to the initial components
array[components] of set of int:subComponents; % Only the components one subassembly down



array[machines] of set of int: tasksOutOfRange;


%-----------------------------------------------------------------------------------%

% -------------- Filtering -------------- %
% -------------- Domain Filtering -------------- %
array[tasks] of int: maxMoveDurs = [max([timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j])|task in tasks];
array[tasks] of int: minMoveDurs = [min([timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j])|task in tasks];
int: maxEnd = sum(duration)+sum(maxMoveDurs);
int: minEnd = (sum(duration)+sum(minMoveDurs)) div nbrMachines;
constraint
  if tempFilter then
    forall(task in 1..nbrTasks+nbrMachines*2)(
      start[task] <= maxEnd-duration[task]
    )
    /\
    forall(task in tasks)(
      start[task] >= minMoveDurs[task]
      /\
      moveStart[task] <= maxEnd-(duration[task]+minMoveDurs[task])
    )
    /\
    makespan <= maxEnd /\ makespan >= minEnd
  else
    true
  endif;

% Limit the duration domains
constraint
  if tempFilter then
  forall(task in tasks)(
    let{
      set of int:nonValidDurs =  0..maxMoveDurs[task] diff {timeMatrix3D[task,j,k]|j in tasks, k in 1..timeMatrix3DDepth where task != j}
    }in
      forall(i in nonValidDurs)(
        moveDuration[task] != i
      )
  )
  else
    true
  endif;

% The assembly has to start with a take task
constraint
  if tempFilter then
  let{
    int:minTakeDur = min([duration[task]+minMoveDurs[task]|task in taking])
  }in
  forall(task in tasks where not(task in taking))(
    moveStart[task] >= minTakeDur
  )
  else
    true
  endif;

% Limit the domains
% Lower bound
constraint
  if tempFilter then
  forall(t in tasks)(
    let{
      set of int: prevTasks = {task|task in tasks where componentCreated[task] in componentsUsed[t]}
    }in
    if card(prevTasks) > 0 then
      if nbrMachines >= card(prevTasks) then
        % if the number of machines are greater than the number of task preceeding task t, then the best scheduling is to do all tasks in parallell. If so the earliest task t can start is equal to the maximum of the earlier tasks
        start[t] >= max({duration[task]+minMoveDurs[task]|task in prevTasks})
      else
        % If the number of machines are less than the number of tasks preceeding task t, then the best we can do is to divide the task times equally on all machines. If the tasks can be divided onto the machines so that the total length of the times on all machines are the same, that time will be equal to sum(prevTasks)/nbrMachines. If they don't match up the maximum of these times will be larger than sum(prevTasks)/nbrMachines.
        start[t] >= sum(task in prevTasks)(duration[task]+minMoveDurs[task]) div nbrMachines
      endif
    else
      true
    endif
  )
  else
    true
  endif;

% Upper bound of task using components later used as subcomponents
constraint
  if tempFilter then
  forall(precTask in tasks)(
    let{
      set of int: succTasks = {task|task in tasks where componentsUsed[precTask] subset taskCompleteSubComponents[task] /\ componentsUsed[precTask] union taskCompleteSubComponents[task] != {}}
    }in
    if card(succTasks) > 0 then
      if nbrMachines >= card(succTasks) then
        % if the number of machines are greater than the number of task succeeding task precTask, then the best scheduling is to do all tasks in parallell. If so the latest task precTask can end is equal to maxEnd minus the maximum of the earlier tasks
        start[precTask] <= maxEnd - max({duration[task]+minMoveDurs[task]|task in succTasks}) - duration[precTask]
      else
        % If the number of machines are less than the number of tasks succeeding task precTask, then the best we can do is to divide the task times equally on all machines. If the tasks can be divided onto the machines so that the total length of the times on all machines are the same, that time will be equal to sum(succTasks)/nbrMachines. If they don't match up the maximum of these times will be larger than sum(succTasks)/nbrMachines.
        start[precTask] <= maxEnd - sum(task in succTasks)(duration[task]+minMoveDurs[task]) div nbrMachines - duration[precTask]
      endif
    else
      true
    endif
  )
  else
    true
  endif;

% -------------- Predecessor Filtering -------------- %

% A task can only be the predecessor to 1 task
constraint
  if predFilter then
  alldifferent(pred)::domain
  else
    true
  endif;

% Take tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  if predFilter then
  forall(takeTask1,takeTask2 in taking where takeTask1 != takeTask2)(
    pred[takeTask1] != takeTask2
  )
  else
    true
  endif;

% Mount tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  if predFilter then
  forall(mountTask1,mountTask2 in mounting where mountTask1 != mountTask2)(
    pred[mountTask1] != mountTask2
  )
  else
    true
  endif;

% Put tasks cannot be predecessors of each other
% REMOVE IF USING EJENSTAMS DATA
constraint
  if predFilter then
  forall(putTask1,putTask2 in putting where putTask1 != putTask2)(
    pred[putTask1] != putTask2
  )
  else
    true
  endif;

% A task1 cannot have task2 as predecessor if task2 uses a component or creates a component that has the component task1 uses as a subcomponent
constraint
  if predFilter then
  forall(task in tasks)(
    let{
      set of int: nonPredecessors = {t|t in tasks where componentsUsed[task] subset taskCompleteSubComponents[t]}
    }in
      forall(nonPred in nonPredecessors)(
        pred[task] != nonPred
      )
  )
  else
    true
  endif;

% Put tasks cannot be first in a circuit
constraint
  if predFilter then
  forall(putTask in putting)(
    forall(startTask in startTasks)(
      pred[putTask] != startTask
    )
  )
  else
    true
  endif;

% Mount tasks cannot be first in a circuit
constraint
  if predFilter then
  forall(mountTask in mounting)(
    forall(startTask in startTasks)(
      pred[mountTask] != startTask
    )
  )
  else
    true
  endif;

% Take tasks cannot be last in a circuit
constraint
  if predFilter then
  forall(takeTask in taking)(
    forall(goalTask in goalTasks)(
      pred[goalTask] != takeTask
    )
  )
  else
    true
  endif;


% One of the output tasks has to be last on one of the circuits
constraint
  if predFilter then
  let{
    int: nbrGoalPreds = length([pred[task]|task in goalTasks]),
    array[1..nbrGoalPreds] of var int:goalPreds = [pred[task]|task in goalTasks],
    int: nbrOutputTasks = length([task|task in tasks where out_put[task] > 0]),
    array[1..nbrOutputTasks] of int: outputTasks = [task|task in tasks where out_put[task] > 0],
    array[1..nbrOutputTasks] of var 0..1: counts
  }in
    global_cardinality(goalPreds, outputTasks, counts)
    /\
    sum(counts) > 0
  else
    true
  endif;

% One of the take tasks, thats not on an output, has to be first on one of the circuits
constraint
  if predFilter then
  let{
    int: nbrTakePreds = length([pred[task]|task in taking where out_put[task] = 0]),
    array[1..nbrTakePreds] of var int:takePreds = [pred[task]|task in taking],
    int: nbrStartTasks = card(startTasks),
    array[1..nbrStartTasks] of int: startTasksArray = [task|task in startTasks],
    array[1..nbrStartTasks] of var 0..1: counts
  }in
    global_cardinality(takePreds, startTasksArray, counts)
    /\
    sum(counts) > 0
  else
    true
  endif;

% If there is a set of concurrent tasks on a subset of tasks using as many machines as available, the tasks after the concurrent tasks cannot have the tasks before the concurrent tasks as predecessors
constraint
if predFilter then
  forall(concGroup in concurrentTasks where card(concGroup) = nbrMachines)(
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
  )
else
true
endif;

% -------------- Constraints -------------- %

% Just printing names
constraint
  forall(task in tasks)(
    trace(show(task)++":"++show(name[task])++"\n")
  );

% All ends has to be lesser than the total end
constraint
  forall(task in tasks)(
    end[task] <= makespan
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

% Each start and end task gets assigned to a machine
constraint
  forall(machine in machines)(
    usingMachine[nbrTasks+machine] = machine
    /\
    usingMachine[nbrTasks+nbrMachines+machine] = machine
  );



% Precedences

% If a set of tasks on a component involves a mount task and a put task, the put task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(put_task in putting_c[comp])(
        end[put_task] <= moveStart[mount_task]
      )
    )
  );

% If a set of tasks on a component involves a mount task and a take task, the take task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
        end[take_task] <= moveStart[mount_task] 
        /\
        if predFilter then
          pred[take_task] != mount_task
        else
          true
        endif
      )
    )
  );

% If a component has a put and take performed on it in a tray, the put has to come before the take
constraint
  forall(comp in components)(
    forall(putTask in putting_c[comp] where tray[putTask] > 0)(
      forall(takeTask in taking_c[comp] where tray[putTask] = tray[takeTask])(
        end[putTask] <= moveStart[takeTask] 
        /\ 
        if predFilter then
          pred[putTask] != takeTask
        else
          true
        endif
      )
    )
  );




% For every put action on a fixture, there is a take action. The put action has to come before the take action
constraint
  forall(f in fixtures)(
    forall(putTask in putting where fixture[putTask] = f)(
      forall(takeTask in taking where fixture[takeTask] = f /\ componentsUsed[putTask] subset taskSubComponents[takeTask])(
        end[putTask] <= moveStart[takeTask] 
        /\
        if predFilter then
          pred[putTask] != takeTask
        else
          true
        endif
      )
    )
  );

% Limitation: This constraint only allows two parts to be mounted before it has to be taken from the fixture. For example, it cannot handle the scenario of mounting two switches into the bottom component before taking the assembled component from the fixture. This is due to the composition of the subcomponent array.
% Fix: Might be fixed now
% The intervals between when components are put and then taken again cannot overlap on the same fixture

constraint
  forall(f in fixtures)(
      let{
        int: nbrPuts = length([trace("*** "++show(task)++"\n", task)|task in putting where fixture[task] = f]),
        array[1..nbrPuts] of int: puts = [trace("=== "++show(task)++"\n", task)|task in putting where fixture[task] = f],
        array[1..nbrPuts] of set of int: takesForEachPut = [trace("+++ "++show({take|take in taking where fixture[take] = f /\ componentsUsed[putTask] subset taskCompleteSubComponents[take]})++"\n", {take|take in taking where fixture[take] = f /\ componentsUsed[putTask] subset taskCompleteSubComponents[take]})|putTask in puts],
        array[1..nbrPuts] of int: takes = [[take|take in takesForEachPut[p] where card(taskCompleteSubComponents[take]) = min([card(taskCompleteSubComponents[t])|t in takesForEachPut[p]])][1] |  p in 1..nbrPuts]
      } in
        cumulative([moveStart[task]|task in puts], [abs(end[takes[i]]-moveStart[puts[i]])|i in 1..nbrPuts], [1|i in 1..nbrPuts], 1)
  );


% Concurrent tasks
constraint
  forall(group in 1..nbrConcurrentGroups)(
    forall(task1 in concurrentTasks[group])(
      forall(task2 in concurrentTasks[group] diff {task1})(
        start[task1] = start[task2] /\ usingMachine[task1] != usingMachine[task2]
        /\
        if predFilter then
          pred[task1] != task2 /\ pred[task2] != task1
        else
          true
        endif
      )
    )
  );

% Components cannot be used before they are created
constraint
  forall(task1 in tasks where componentCreated[task1] > 0)(
    forall(task2 in tasks where componentCreated[task1] in componentsUsed[task2])(
      moveStart[task2] >= end[task1]
      /\
      if predFilter then
        pred[task1] != task2
      else
        true
      endif
    )
  );

% Task using a component cannot execute before all the tasks having it as subcomponent
constraint
  forall(precTask in tasks)(
    forall(task in tasks where precTask != task /\ componentsUsed[precTask] union taskCompleteSubComponents[task] != {} /\ componentsUsed[precTask] union taskCompleteSubComponents[task] subset taskCompleteSubComponents[task])(
      end[precTask] <= moveStart[task]
      /\
      if predFilter then
        pred[precTask] != task
      else
        true
      endif
    )
  );


% Tasks on the same fixture can't overlap
constraint
  forall(f in fixtures)(
    let{
      int:nbrFixtureTasks = length([task|task in tasks where fixture[task] = f]),
      array[1..nbrFixtureTasks] of int:fixtureTasks = [task|task in tasks where fixture[task] = f]
    }in
      cumulative([start[task]|task in fixtureTasks],[duration[task]|task in fixtureTasks],[1|task in fixtureTasks],1)
  );

% Tasks on the same tray can't overlap
constraint
  forall(t in trays)(
    let{
      int:nbrTrayTasks = length([task|task in tasks where tray[task] = t]),
      array[1..nbrTrayTasks] of int:trayTasks = [task|task in tasks where tray[task] = t]
    }in
      cumulative([start[task]|task in trayTasks],[duration[task]|task in trayTasks],[1|task in trayTasks],1)
  );

% Tasks on the same output can't overlap
constraint
  forall(o in outputs)(
    let{
      int:nbrOutputTasks = length([task|task in tasks where out_put[task] = o]),
      array[1..nbrOutputTasks] of int:outputTasks = [task|task in tasks where out_put[task] = o]
    }in
      cumulative([start[task]|task in outputTasks],[duration[task]|task in outputTasks],[1|task in outputTasks],1)
  );

% A task can only start after the move to it
constraint
  forall(task in tasks)(
    start[task] >= moveEnd[task]
  );


% -------------- Predecessors -------------- %

% A task has to start after its predecessor
constraint
   forall(task in tasks)(
      moveStart[task] >= end[pred[task]]
  );


% For all start tasks except the first one
constraint  
  forall(startTask in startTasks diff {nbrTasks+1})(
    % The start tasks predecessor is the previous goal task
    pred[startTask] = startTask+nbrMachines-1
  );

% The first start tasks predecessor is the last goal task
constraint
  let{
    int: firstStartTask = nbrTasks+1,
    int: lastGoalTask = nbrTasks+nbrMachines*2
  }in
  pred[firstStartTask] = lastGoalTask;


% The predecessors has to form a circuit
constraint
  circuit(pred);


% If a set of tasks on a component involves a mount and a take task, but no move tasks or put task on the same fixture, tray or output as the mount, the take task is the predecessor of the mount task
constraint
  forall(comp in components)(
    forall(mountTask in mounting_c[comp])(
      let{
        set of int: puts = {p|p in putting_c[comp] where (fixture[p] > 0 /\ fixture[p] = fixture[mountTask]) \/ (out_put[p] > 0 /\ out_put[p] = out_put[mountTask]) \/ (tray[p] > 0 /\ tray[p] = tray[mountTask])}
      } in
      forall(takeTask in taking_c[comp] where not(takeTask in orderedSet) /\ puts = {})(
          pred[mountTask] = takeTask
      )
    )
  );


% If a set of tasks on a component involves a put task not in a tray and a take task, and there is no moves involved, the take task has to be the predecessor of the put task
constraint
  forall(comp in components where moving_c[comp] = {})(
    forall(putTask in putting_c[comp] where tray[putTask] = 0)(
      forall(takeTask in taking_c[comp])(
        pred[putTask] = takeTask
      )
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
    )
  );


% A task has to use the same machine as its predecessor
constraint
  forall(task in tasks union goalTasks)(
    usingMachine[task] = usingMachine[pred[task]]
  );

% Take tasks has to use the same tool as its predecessor or do a change first
constraint
  forall(task in tasks)(
    moveDuration[task] = timeMatrix3D[pred[task],task,abs(toolUsed[task]-toolUsed[pred[task]])+1]
  );

constraint
  forall(task in tasks where toolNeeded[task] != 0)(
    toolUsed[task] = toolNeeded[task]
  );



% -------------- Tasks out of range -------------- %


constraint
  forall(machine in machines)(
    forall(task in tasksOutOfRange[machine])(
      usingMachine[task] != machine
    )
  );




solve :: seq_search([
                      int_search(usingMachine, first_fail, indomain_max, complete),
                        int_search([pred[task]|task in taking where out_put[task] = 0], first_fail, indomain_min, complete),
                      int_search([pred[task]|task in putting where out_put[task] = 0], first_fail, indomain_median, complete),
                      int_search([pred[task]|task in mounting where out_put[task] = 0], first_fail, indomain_median, complete),%Maybe change to input order
                      int_search([pred[task]|task in tasks where out_put[task] != 0], first_fail, indomain_median, complete),
                      int_search(pred, first_fail, indomain_median, complete),
                      int_search(start, largest, indomain_min, complete)
                     
])
        minimize makespan;

output ["Minimizing = " ++ show(makespan) ++ "\n"]++["End = " ++ show(max(end)) ++ "\n"] ++ 
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

