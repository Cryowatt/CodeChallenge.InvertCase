# Coding Challenge - Invert Case Performance

This is a performance challenge. The task is simple, read some ASCII words from stdin, invert the case (`'aBcDeF' => 'AbCdEf'`), then push the result to stdout. The goal is to beat the performance of the baseline dotnet core implementation.

## Rules

The only rule is you cannot modify the benchmarking application code. Any other extreme optimizations or 'creative' solutions are allowed. For those attempting 'creative' solutions, I'd also suggest providing a 'real' solution as well. I'm being intetionally vague here.

## Protocol

There is a very simply protocol between the benchmarking app and the target application.

1. Recieve `"#BEGIN\r\n"`
    * Benchmarking application wishes to start a bencharmking pass.
2. Send `"#READY\r\n"`
    * Target application does whatever warmup and prep processing required before returning this signal
    * After the ready signal is recieved, the benchmarking timer begins
3. Send words (example: `"AbCdEf\r\n"`)
    * Words are sent in 64kb chunks
    * Sends are asynchronous, the benchmarking application will consume results as they are returned
4. Recieve inverted case words (example: `"abCdEf\r\n"`)
    * Make sure you are appending `\r\n` with the results. This is not the default on most platforms.
5. Send `"#END {n}\r\n"`
    * `n` is the pass number (example: `"#END 0\r\n"`)
    * The benchmarking application will now stop the benchmarking timer and verify the results
    * If the results are incorrect, errors will be returned
6. GOTO 1
    * Benchmarking application will run 10 passes
    * Slowest pass will be thrown out
    * Results are reported as a percentage of the baseline application

## How?

First, Fork the repository.

Next, replace the `CodeChallenge.Target` implementation with your own version. You aren't limited to C# and dotnet for this. Anything that will execute in a docker container is eligable. Write your solution in javascript, go, rust, webassembly or any other language you want. Just update `fixture.Dockerfile` to include an appropriate build/runtime environment for your language of choice.

You can run `docker-compose run debug` to test your implementation. All stdout and stderr streams from your application will be echoed to the console. When you're ready to run for real just run `docker-compose run benchmark`. The benchmark will run the baseline first and your implementation next. You'll recieve a list of run times for each pass and a final score.

## Questions

* It looks like the benchmark app is freezing, what's going on?
    * Most likely you've missed the `\r\n` characters after a message. These are required even on linux.
* Why did you do `[x]` like that?
    * I've set this up as an educational project, so the protocol design was done with a specific implementation in mind.
* How will submissions be ranked?
    * 'Creative' and real solutions are ranked independantly
    * Fast solution is the winner
