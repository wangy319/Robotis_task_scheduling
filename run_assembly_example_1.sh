cd MiniZinc;
mzn-g12fd -a -s Assembly_example_1.mzn Assembly_example_1.dzn -o ../Assemble_example_1_dependencies_output;
cd ..;
java -jar SchedPrinter.jar Assemble_example_1_dependencies_output > Assemble_example_1_visual;
