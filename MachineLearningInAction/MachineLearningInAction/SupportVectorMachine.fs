﻿namespace MachineLearning

module SupportVectorMachine =

    // an observation, its label, and current alpha estimate
    type Row = { Data: float list; Label: float; Alpha: float }
    // SVM algorithm input parameters
    type Parameters = { Tolerance: float; C: float; Depth: int }

    type Attempt<'a> = Success of 'a | Failure

    // limit for what is considered too small a change
    let smallChange = 0.00001

    // Product of vectors
    let dot (vec1: float list) 
            (vec2: float list) =
        List.fold2 (fun acc v1 v2 -> 
            acc + v1 * v2) 0.0 vec1 vec2

    // Clip a value x that is out of the min/max bounds
    let clip (min, max) x =
        if (x > max)
        then max
        elif (x < min)
        then min
        else x

    let findLowHigh low high row1 row2 = 
        if row1.Label = row2.Label
        then max low (row1.Alpha + row2.Alpha - high), min high (row2.Alpha + row1.Alpha)
        else max low (row2.Alpha - row1.Alpha),        min high (high + row2.Alpha - row1.Alpha) 
    
    // next index "around the clock"
    let nextAround size i = (i + 1) % size

    // compute error on row, given current alphas
    let rowError rows b row =
        rows
        |> Seq.filter (fun r -> r.Alpha > 0.0)
        |> Seq.fold (fun acc r -> 
            acc + r.Label * r.Alpha * (dot r.Data row.Data)) (b - row.Label)

    // check whether alpha can be modified for row,
    // given error and parameter C
    let canChange parameters row error =
        if (error * row.Label < - parameters.Tolerance && row.Alpha < parameters.C)
        then true
        elif (error * row.Label > parameters.Tolerance && row.Alpha > 0.0)
        then true
        else false

    let updateB b rowI rowJ iAlphaNew jAlphaNew iError jError C =

        let b1 = b - iError - rowI.Label * (iAlphaNew - rowI.Alpha) * (dot rowI.Data rowI.Data) - rowJ.Label * (jAlphaNew - rowJ.Alpha) * (dot rowI.Data rowJ.Data)
        let b2 = b - jError - rowI.Label * (iAlphaNew - rowI.Alpha) * (dot rowI.Data rowJ.Data) - rowJ.Label * (jAlphaNew - rowJ.Alpha) * (dot rowJ.Data rowJ.Data)

        if (0.0 < iAlphaNew && iAlphaNew < C)
        then b1
        elif (0.0 < jAlphaNew && jAlphaNew < C)
        then b2
        else (b1 + b2) / 2.0

    // Attempt to update support vectors i and j
    let pivot (rows: Row list) b parameters i j =
    
        printfn "%i %i" i j
    
        let rowi = rows.[i]
        let iError = rowError rows b rowi
    
        if not (canChange parameters rowi iError)
        then Failure
        else
            let rowj = rows.[j]
            let lo, hi = findLowHigh 0.0 parameters.C rowi rowj

            if lo = hi 
            then Failure
            else
                let eta = 2.0 * dot rowi.Data rowj.Data - dot rowi.Data rowi.Data - dot rowj.Data rowj.Data
            
                if eta >= 0.0 
                then Failure
                else   
                    let jError = rowError rows b rowj

                    let jAlphaNew = clip (lo, hi) (rowj.Alpha - (rowj.Label * (iError - jError) / eta))

                    if (abs (jAlphaNew - rowj.Alpha) < smallChange)
                    then Failure
                    else
                        let iAlphaNew = rowi.Alpha + (rowi.Label * rowj.Label * (rowj.Alpha - jAlphaNew))
                        let updatedB = updateB b rowi rowj iAlphaNew jAlphaNew iError jError parameters.C

                        printfn "First: %f -> %f" rowi.Alpha iAlphaNew
                        printfn "Second: %f -> %f" rowj.Alpha jAlphaNew
                        printfn "B: %f -> %f" b updatedB

                        let updatedRows =
                            rows 
                            |> List.mapi (fun index value -> 
                                if index = i 
                                then { value with Alpha = iAlphaNew } 
                                elif index = j 
                                then { value with Alpha = jAlphaNew }
                                else value)
                        Success(updatedRows, updatedB) 