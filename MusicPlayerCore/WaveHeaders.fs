module WaveFormat 
    open System

    let getHeaders tracks bitsPerSample samplesPerSecond msDuration =
        let formatChunkSize = 16
        let headerSize = 8
        let formatType= 1s
        let frameSize = tracks * ((bitsPerSample + 7s) / 8s)
        let bytesPerSecond = samplesPerSecond * (int)frameSize
        let waveSize = 4;
        let samples = (int)(samplesPerSecond * msDuration / 1000)
        let dataChunkSize = samples * (int)frameSize;
        let fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
        seq {
            yield [|'R'B ; 'I'B; 'F'B;'F'B |]
            yield (BitConverter.GetBytes(fileSize))
            yield [|'W'B ; 'A'B; 'V'B;'E'B |]
            yield [|'f'B ; 'm'B; 't'B; ' 'B|] 
            yield (BitConverter.GetBytes(formatChunkSize))
            yield (BitConverter.GetBytes(formatType))
            yield (BitConverter.GetBytes(tracks))
            yield (BitConverter.GetBytes(samplesPerSecond))
            yield (BitConverter.GetBytes(bytesPerSecond))
            yield (BitConverter.GetBytes(frameSize))
            yield (BitConverter.GetBytes(bitsPerSample))
            yield [|'d'B ; 'a'B; 't'B;'a'B |]
            yield BitConverter.GetBytes(dataChunkSize)
         } |> Array.concat



