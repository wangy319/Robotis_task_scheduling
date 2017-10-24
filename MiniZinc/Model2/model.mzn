include "cumulative.mzn";
include "member.mzn";
include "alldifferent_except_0.mzn";

int:nbrTasks;
int:nbrMachines;
int:nbrTools;
int:nbrComponents;
int:nbrCycles;
int:nbrUniqueTasks = nbrTasks div nbrCycles;
set of int: tasks = 1..nbrTasks;
set of int: machines = 1..nbrMachines;
set of int: tools = 1..nbrTools;

array[tasks] of string:names;
array[tasks] of int:duration;
array[1..nbrUniqueTasks] of set of int:identicalTasks;
array[tasks] of int:cycleNbr;
array[tasks] of int:tray;
array[tasks] of int:fixture;
array[tasks] of set of int:componentsUsed;

array[tasks] of int:component_created;

set of int:putting;
set of int:mounting;
set of int:taking;
set of int:moving;

int:nbrConcurrentGroups;
array[1..nbrConcurrentGroups] of set of int:concurrentTasks;

int:orderedGroups;
int:maxOrderedLength;
array[1..orderedGroups,1..maxOrderedLength] of int:order;

%array[1..nbrTasks+1,1..4] of int:coords;

array[tasks] of int: tools_needed; % The tool nedded for each task. 0 means no specific tool is required

set of int: changesTool;
array[tasks] of int: changesToTool;
set of int:toolsChangedTo = {changesToTool[changeTask]|changeTask in changesTool}; % A set of the tools that there is a change to. i.e. it does not include the default tools

array[machines] of int: default_tool; % The default tool for each machine

set of int:fixtures = {fixture[i]|i in tasks where fixture[i] > 0}; % The set of fixtures available
set of int:trays = {tray[i]|i in tasks where tray[i] > 0}; % The set of trays available
set of int:components = 1..nbrComponents;
set of int:cycles = {cycleNbr[i]|i in tasks}; % The set of cycles used
array[components] of set of int:putting_c = [{task|task in putting where c in componentsUsed[task]}|c in components];
array[components] of set of int:mounting_c = [{task|task in mounting where c in componentsUsed[task]}|c in components];
array[components] of set of int:taking_c = [{task|task in taking where c in componentsUsed[task]}|c in components];
array[components] of set of int:moving_c = [{task|task in moving where c in componentsUsed[task]}|c in components];

array[tasks] of var machines: usingMachine; % The machine each task uses
array[1..nbrTasks+1,tasks] of int:times; % The time it takes to move from one task to another
array[tasks] of var 0..nbrTasks:pred; % The predecessor task for each task. The first tasks has predecessor 0

int: total = sum(i in tasks)(duration[i]);
array [tasks] of var 0..total*2: start;
array [tasks] of var 0..total*2: end;
var 0..total*2: total_end;

array[tasks] of var 0..total*2:moveStart;
array[tasks] of var 0..total*2:moveDuration;
array[tasks] of var 0..total*2:moveEnd;

%-----------------------------------------------------------------------------------%
% Predicates
predicate no_overlap(int:task1, int:task2) =
  no_overlap_2(start[task1], end[task1], start[task2], end[task2]);

predicate no_overlap_2(var int:start1, var int:end1, var int:start2, var int:end2) =
  end1 <= start2 \/ start1 >= end2;

predicate task1EndBeforeTask2Start(int:task1, int:task2) =
  moveEnd[task1] <= start[task2] /\ end[task1] <= start[task2] /\ end[task1] <= moveStart[task2] /\ moveEnd[task1] <= moveStart[task2] /\ pred[task1] != task2;

predicate task1EndBeforeTask2Start(var int:task1, int:task2) =
  moveEnd[task1] <= start[task2] /\ end[task1] <= start[task2] /\ end[task1] <= moveStart[task2] /\ moveEnd[task1] <= moveStart[task2] /\ pred[task1] != task2;

predicate task1PredecessorOfTask2(int:task1, int:task2) =
  pred[task2] = task1 /\ pred[task1] != task2 /\ pred[task1] != pred[task2];

%-----------------------------------------------------------------------------------%
%Constraints

% The end is were the last task ends
constraint
  total_end = max(end);

% End times
constraint
  forall(i in tasks)(
    end[i] = start[i] + duration[i]
  );

constraint
  forall(i in tasks)(
    moveEnd[i] = moveStart[i] + moveDuration[i]
  );

% Create the times matrix
% constraint 
%   forall(to in tasks)(
%     forall(from in 1..nbrTasks+1)(
%     let{
%       int:x = abs(coords[to+1,1]-coords[from,3]),
%       int:y = abs(coords[to+1,2]-coords[from,4])
%     } in
%     times[from,to] = round(sqrt(int2float(pow(x,2)+pow(y,2)))/1.0) % 5.0 is an approximation of armspeed in points/second
%     )
%   );

% Precedences
% If a set of tasks on a component involves a mount task and a put task, the put task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(put_task in putting_c[comp])(
        task1EndBeforeTask2Start(put_task, mount_task)
      )
    )
  );

% If a set of tasks on a component involves a mount and a take task, but no move tasks or put task, the take task is the predecessor of the mount task
constraint
  forall(comp in components where putting_c[comp] = {} /\ moving_c[comp] = {})(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
          task1PredecessorOfTask2(take_task, mount_task)
      )
    )
  );

% If a set of tasks on a component involves a mount task, a take task, and one or several moving tasks, but no put task, the take task has to come before the mount task, and the move tasks come inbetween
constraint
  forall(comp in components where putting_c[comp] = {} /\ moving_c[comp] != {})(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
          task1EndBeforeTask2Start(take_task, mount_task) /\ 
          pred[mount_task] != take_task /\
          usingMachine[take_task] = usingMachine[mount_task] /\
          % The take task has to happen before the moving tasks and the mount task afterwards
          forall(task in moving_c[comp])(
            task1EndBeforeTask2Start(take_task, task) /\ task1EndBeforeTask2Start(task, mount_task)
          )
          /\
          % No other non-move task can come between the take task and the mount
          forall(task in tasks diff {mount_task, take_task} where not(task in moving))(
            usingMachine[take_task] = usingMachine[task] -> no_overlap_2(start[task], end[task],  start[take_task], end[mount_task])
          )
      )
    )
  );

% If a set of tasks on a component involves a put task in a tray and a take task, the take task ha to be the predecessor of the put task
constraint
  forall(comp in components)(
    forall(put_task in putting_c[comp] where tray[put_task] = 0)(
      forall(take_task in taking_c[comp])(
        task1PredecessorOfTask2(take_task, put_task)
      )
    )
  );

% Concurrent tasks
constraint
  forall(group in 1..nbrConcurrentGroups)(
    forall(task1 in concurrentTasks[group])(
      forall(task2 in concurrentTasks[group] diff {task1})(
        start[task1] = start[task2] /\ usingMachine[task1] != usingMachine[task2] /\ pred[task1] != task2 /\ pred[task2] != task1 /\ pred[task1] != pred[task2]
      )
    )
  );

% Components cannot be used before they are created
constraint
  forall(task1 in tasks where component_created[task1] > 0)(
    forall(task2 in tasks where component_created[task1] in componentsUsed[task2])(
      task1EndBeforeTask2Start(task1, task2)
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
       task1PredecessorOfTask2(task1, task2)
    )
  );

% Two tasks on the same fixture can't overlap
constraint
  forall(f in fixtures)(
    forall(task1 in tasks where fixture[task1] = f)(
      forall(task2 in tasks diff {task1} where fixture[task1] = fixture[task2])(
        no_overlap(task1, task2)
      )
    )
  );

% Two tasks on the same tray can't overlap
constraint
  forall(t in trays)(
    forall(task1 in tasks where tray[task1] = t)(
      forall(task2 in tasks diff {task1} where tray[task1] = tray[task2])(
        no_overlap(task1, task2)
      )
    )
  );

% Two tasks using the same machine cannot overlap
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] = usingMachine[task2] -> no_overlap(task1, task2)
    )
  );

% There can only be as many tasks performed simultaneously as there are machines
 constraint
   cumulative(start, duration, [1|t in tasks], nbrMachines);


% Predecessors

% A task cannot be a predecessor to itself
constraint
  forall(task in tasks)(
    pred[task] != task
  );

% A task can only start after the move to it
constraint
  forall(task in tasks)(
    start[task] >= moveEnd[task]
  );

% Apart from the first tasks, a task has to start after its predecessor
constraint
   forall(task in tasks)(
    pred[task] != 0 -> task1EndBeforeTask2Start(pred[task], task)
  );

% A task cannot be a predecessor to its predecessor
constraint
  forall(task in tasks)(
    pred[task] != 0 -> pred[pred[task]] != task
  );

% Since there are multiple machines, there can be multiple start tasks
% Unnecessary?
constraint
  alldifferent_except_0(pred);

% Tasks on the same machine cannot have the same predecessor
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] = usingMachine[task2] -> pred[task1] != pred[task2]
    )
  );

% The start tasks have predecessor 0 and starts at time 0
constraint
  forall(task in tasks)(
    pred[task] = 0 -> moveStart[task] = 0
  );


% Two tasks using different machines cannot be each other predecessors
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] != usingMachine[task2] -> pred[task1] != task2 /\ pred[task2] != task1
    )
  );

% A task and its predecessor has to use the same machine
constraint
  forall(task in tasks)(
    pred[task] != 0 -> usingMachine[task] = usingMachine[pred[task]]
  );

% There has to be at least one starting task
constraint
  member(pred,0);

% No other task on the same machine can come between task i and its predecessor pred[i], i.e. no other task can overlap the timeslot inbetween
constraint
  forall(task in tasks)(
    pred[task] != 0 -> forall(task2 in tasks)( usingMachine[task2] = usingMachine[task] -> no_overlap_2(start[task2], end[task2], end[pred[task]], start[task]) )
  );

% Assigns the duration for the move between tasks
constraint
  forall(task in tasks)(
    moveDuration[task] = times[pred[task]+1,task]
  );


% Tools

% A tool-change cannot happen right after a take task, i.e. a tool-change cannot have a take task as it predecessor
constraint
  forall(take_task in taking, changeTask in changesTool)(
      pred [changeTask] != take_task
  );

% Tasks needing a tool has to use the same machine as a task switching to that tool
constraint
  forall(use_task in tasks where tools_needed[use_task] != 0)(
    exists(changeTask in changesTool where changesToTool[changeTask] = tools_needed[use_task])(
      usingMachine[use_task] = usingMachine[changeTask] /\ task1EndBeforeTask2Start(changeTask, use_task)
    )
    \/
    exists(machine in machines where default_tool[machine] = tools_needed[use_task])(
      usingMachine[use_task] = machine /\ start[use_task] >= 0
    )
  );

% A task that needs a tool needs to happen before the change to another tool on the machine it is using
constraint
  forall(use_task in tasks where tools_needed[use_task] != 0 /\ toolsChangedTo diff {tools_needed[use_task]} != {})(
    exists(changeTask in changesTool where changesToTool[changeTask] != tools_needed[use_task])(
      usingMachine[use_task] = usingMachine[changeTask] -> task1EndBeforeTask2Start(use_task, changeTask)
    )
  );

% Spatial constraint
% constraint
%   forall(task1 in tasks)(
%     forall(task2 in tasks diff {task1})(
%       if {1|i in 1..nbrConcurrentGroups where task1 in concurrentTasks[i] /\ task2 in concurrentTasks[i]} = {} /\
% ((coords[task1+1,1] = coords[task2+1,1] /\ coords[task1+1,2] = coords[task2+1,2]) \/ (coords[task1+1,3] = coords[task2+1,3] /\ coords[task1+1,4] = coords[task2+1,4])) then
%         no_overlap_2(moveStart[task1], end[task1], moveStart[task2], end[task2])
%       else true endif
%     )
%   );

% Cycle order
constraint
  forall(it in identicalTasks)(
    forall(task1 in it)(
      forall(task2 in it where cycleNbr[task1] < cycleNbr[task2])(
          task1EndBeforeTask2Start(task1, task2)
      )
    )
  );

solve :: seq_search([
                      int_search(usingMachine, first_fail, indomain, complete),
                      int_search(pred, first_fail, indomain, complete),
                      int_search(start, first_fail, indomain_split, complete),
                      int_search(moveStart, first_fail, indomain_split, complete)
])
        minimize total_end;
% solve minimize total_end;

output ["End = " ++ show(total_end) ++ "\n"] ++ 
[show(moveStart[i]) ++ " " ++ show(moveDuration[i]) ++ " 0 0 0" ++ " Move from " ++ show(pred[i]) ++ " to " ++ show(i) ++ "\n" ++
show(start[i]) ++ " " ++ show(duration[i]) ++ " " ++ show(i) ++ " " ++ show(usingMachine[i]) ++ " " ++ show(pred[i]) ++ " " ++ show(names[i]) ++ "\n"|i in tasks];

