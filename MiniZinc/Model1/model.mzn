int:tasks;
int:machines;
array[1..tasks] of string:names;
array[1..tasks] of int:d; % Durations
array[1..tasks,1..tasks] of int:dep; % Dependencies
array[1..tasks] of var 1..machines: m;

array[1..tasks] of int: tools_needed; % The tool nedded for each task. 0 means no specific tool is required
int: tool_changes; % The number of tool changes in changes_tool. The width of the matrix
array[1..2,1..tool_changes] of int: changes_tool; % Maps the task changing to a tool to the tool it is changing to
array[1..machines] of int: default_tool; % The default tool for each machine
set of int:tools_that_do_change; % A set of the tools that there is a change to. i.e. it does not include the default tools

int:max_same_machines; %The with of same_machine
int:nbr_of_same_machines; % The height of same_machine
array[1..nbr_of_same_machines,1..max_same_machines] of int: same_machine; % Operations needing the same machine. Each row i a set of operations needing the same machine. To make a complete matrix, it is filled with 0 where there is no action.

int:max_concurrent_tasks; % The width of concurrent_tasks
int:nbr_concurrent_groups; % The height of concurrent_tasks
array[1..nbr_concurrent_groups,1..max_concurrent_tasks] of int:concurrent_tasks; % Tasks that needs to be executed concurrently. To make a complete matrix, it is filled with 0 where there is no action.

int:max_occupation; % The width of occupied
int:nbr_occupations; % The height of occupied
array[1..nbr_occupations,1..max_occupation] of int:occupied; % Tasks that occupies the same machine in the order specified by dep. To make a complete matrix, it is filled with 0 where there is no action.

int: total = sum(i in 1..tasks)(d[i]);
array [1..tasks] of var 0..total: s;
var 0..total: end;


%-----------------------------------------------------------------------------------%
%Constraints

% The end is were the last task ends
constraint
  end = max([s[task] + d[task]|task in 1..tasks]);

% Restrict the order
constraint
  forall(task1 in 1..tasks, task2 in 1..tasks where dep[task1,task2] = 1)(s[task1]+d[task1] <= s[task2]);

% Restrict the resources
% Operation overlaping can't use the same machine
constraint
  forall(task1 in 1..tasks, task2 in 1..tasks where task1 != task2)(
  let {
    var int: task1_start = s[task1],
    var int: task1_end = task1_start + d[task1],
    var int: task2_start = s[task2],
    var int: task2_end = task2_start + d[task2],
    var bool: overlap = (task1_start >= task2_start /\ task1_start < task2_end) \/ (task1_end > task2_start /\ task1_end <= task2_end),
    var int: task1_machine = m[task1],
    var int: task2_machine = m[task2]
  } in
    overlap -> task1_machine != task2_machine
  );

% Tools
% Default tools
constraint
  forall(machine in 1..machines)(
    forall(task in 1..tasks where default_tool[machine] = tools_needed[task])(
       m[task] = machine -> s[task] >= 0
    )
  );

% Tasks needing a tool has to use the same machine as a task switching to that tool
constraint
  forall(use_task in 1..tasks where tools_needed[use_task] != 0)(
    let{
      int: tool_needed = tools_needed[use_task]
    } in
    exists(change_task in 1..tool_changes where changes_tool[2,change_task] = tool_needed)(
      m[use_task] = m[change_task]
    )
    \/
    exists(machine in 1..machines where default_tool[machine] = tool_needed)(
      m[use_task] = machine
    )
  );

% Relation to the tasks that changes to the tool a task uses
constraint
  forall(use_task in 1..tasks where tools_needed[use_task] != 0 /\ tools_needed[use_task] in tools_that_do_change)(
  let{
    int: tool_needed = tools_needed[use_task],
    var int: using_tool_start = s[use_task]
  } in
    % A task that needs a tool needs to happen after the change to that tool
    exists(change_task in 1..tool_changes where changes_tool[2,change_task] = tool_needed)(
    let{
      int: tool_change_task = changes_tool[1,change_task],
      var int: machine_using_tool = m[use_task]
    } in
      let{
        var int: machine_changing_tool = m[tool_change_task],
        var int: change_tool_start = s[tool_change_task],
        int: change_tool_duration = d[tool_change_task]
      } in
      machine_using_tool = machine_changing_tool -> using_tool_start >= change_tool_start + change_tool_duration
    )
);

% Relations to the tasks that changes to a tool the task does not use
constraint
  forall(use_task in 1..tasks where tools_needed[use_task] != 0 /\ tools_that_do_change diff {tools_needed[use_task]} != {})(
  let{
    int: tool_needed = tools_needed[use_task],
    var int: using_tool_start = s[use_task],
    int: using_tool_dur = d[use_task]
  } in
    % A task that needs a tool needs to happen before the change to another tool
    exists(change_task in 1..tool_changes where changes_tool[2,change_task] != tool_needed)(
    let{
      int: tool_change_task = changes_tool[1,change_task],
      var int: machine_using_tool = m[use_task]
    } in
      let{
        var int: machine_changing_tool = m[tool_change_task],
        var int: change_tool_start = s[tool_change_task]
      } in
      machine_using_tool = machine_changing_tool -> using_tool_start + using_tool_dur <= change_tool_start
    )
  );


% See to it some operations get the same machine
constraint
  forall(i in 1..nbr_of_same_machines)(
    forall(j in 1..max_same_machines-1 where same_machine[i,j] != 0)(
      forall(k in j+1..max_same_machines where same_machine[i,k] != 0)(
      let{
        int: task1 = same_machine[i,j],
        int: task2 = same_machine[i,k],
        var int: task1_machine = m[task1],
        var int: task2_machine = m[task2]
      } in
        task1_machine = task2_machine
      )
    )
  );

% Simultaneous tasks
constraint
  forall(i in 1..nbr_concurrent_groups)(
    forall(j in 1..max_concurrent_tasks where concurrent_tasks[i,j] != 0)(
      let{
        int:task1 = concurrent_tasks[i,j]
      } in
      forall(k in 1..max_concurrent_tasks where concurrent_tasks[i,k] != 0 /\ k != j)(
        let{
          int:task2 = concurrent_tasks[i,k],
          var int: task1_start = s[task1],
          var int: task2_start = s[task2],
          var int: task1_machine = m[task1],
          var int: task2_machine = m[task2]
        } in
        task1_start = task2_start /\ task1_machine != task2_machine
      )  
    )
  );

% Machine occupation
% There cannot be tasks between the ones occupying a machine that is not specified in occupied
constraint
  forall(i in 1..nbr_occupations)(
    forall(j in 1..max_occupation-1 where occupied[i,j] != 0 /\ occupied[i,j+1] != 0)(
    let{
      int:from_task = occupied[i,j],
      var int: from_task_start = s[from_task],
      int:to_task = occupied[i,j+1],
      var int: to_task_end = s[to_task] + d[to_task],
      var int:machine = m[from_task]
    } in
      forall(task_between in 1..tasks where task_between != from_task /\ task_between != to_task)(
      let{
        var int: task_between_start = s[task_between],
        var int: task_between_end = task_between_start + d[task_between],
        var int: task_between_machine = m[task_between]
      } in
        task_between_machine = machine -> (task_between_start < from_task_start \/ task_between_start >= to_task_end) /\ (task_between_end <= from_task_start \/ task_between_end > to_task_end)
      )
    )
  );




solve minimize end;

output ["End = " ++ show(end) ++ "\n"] ++ [show(s[i]) ++ " " ++ show(d[i]) ++ " " ++ show(names[i]) ++ " m" ++ show(m[i]) ++ "\n"|i in 1..tasks]++[show(m)];

