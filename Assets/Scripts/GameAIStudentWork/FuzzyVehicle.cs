// Remove the line above if you are submitting to GradeScope for a grade. But leave it if you only want to check
// that your code compiles and the autograder can access your public methods.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameAI;

// All the Fuzz
using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;
using Tochas.FuzzyLogic.Utils;
using static Tochas.FuzzyLogic.FuzzyCrossfade;
using static Tochas.FuzzyLogic.FuzzyDiscreteSet;
using static Tochas.FuzzyLogic.FuzzyVisualize; 

namespace GameAI
{

    public class FuzzyVehicle : AIVehicle
    {

        // TODO create some Fuzzy Set enumeration types, and member variables for:
        // Fuzzy Sets (input and output), one or more Fuzzy Value Sets, and Fuzzy
        // Rule Sets for each output.
        // Also, create some methods to instantiate each of the member variables

        // Here are some basic examples to get you started
        enum FzOutputThrottle {Brake, Coast, Accelerate }
        enum FzOutputWheel { TurnLeft, GoStraight, TurnRight }

        enum FzInputSpeed { Slow, Medium, Fast }
        enum FzInputLanePosition { Right, Center, Left }
        enum FzInputFutureCurvature { TowardsLeft, Straight, TowardsRight }

        FuzzySet<FzInputSpeed> fzSpeedSet;
        FuzzySet<FzInputLanePosition> fzLanePositionSet;
        FuzzySet<FzInputFutureCurvature> fzFutureCurvatureSet;

        FuzzySet<FzOutputThrottle> fzThrottleSet;
        FuzzyRuleSet<FzOutputThrottle> fzThrottleRuleSet;

        FuzzySet<FzOutputWheel> fzWheelSet;
        FuzzyRuleSet<FzOutputWheel> fzWheelRuleSet;

        FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

        // These are used for debugging (see ApplyFuzzyRules() call
        // in Update()
        FuzzyValueSet mergedThrottle = new FuzzyValueSet();
        FuzzyValueSet mergedWheel = new FuzzyValueSet();



        private FuzzySet<FzInputSpeed> GetSpeedSet()
        {
            FuzzySet<FzInputSpeed> set = null;

            // TODO: Define this fuzzy input variable using GenerateCrossfadeFuzzySet<T>().
            // Each enum label should have overlapping triangular or trapezoidal DoM functions.
            // Example (pseudocode):
            // set = GenerateCrossfadeFuzzySet<FzInputSpeed>( ... );
            // replace the following:

            // set = new FuzzySet<FzInputSpeed>();
            set = GenerateCrossfadeFuzzySet<FzInputSpeed>((0f, 50f), 90f, (110f, 150f));
            // You can then print the ASCII visualization for debugging:
            // Debug.Log(RenderFuzzySetAscii(set));

            return set;
        }

        private FuzzySet<FzInputLanePosition> GetLanePositionSet()
        {
            FuzzySet<FzInputLanePosition> set = GenerateCrossfadeFuzzySet<FzInputLanePosition>((-4f, -2.9f), 0f, (2.9f, 4f));
            // Debug.Log(RenderFuzzySetAscii(set));

            return set;
        }

        private FuzzySet<FzInputFutureCurvature> GetCurvatureSet()
        {
            FuzzySet<FzInputFutureCurvature> set = GenerateCrossfadeFuzzySet<FzInputFutureCurvature>((-70f, -20f), 0f, (20f, 70f));
            // Debug.Log(RenderFuzzySetAscii(set));

            return set;
        }

        private FuzzySet<FzOutputThrottle> GetThrottleSet()
        {
            FuzzySet<FzOutputThrottle> set = null;

            // TODO: Define this fuzzy output variable using GenerateDiscreteFuzzySet<T>().

            // Example (pseudocode):
            // set = GenerateDiscreteFuzzySet<FzOutputThrottle>( ... );
            // replace the following:

            // set = new FuzzySet<FzOutputThrottle>();
            set = GenerateDiscreteFuzzySet<FzOutputThrottle>(-1f, 0f, 1f);
            // Use RenderFuzzySetAscii(set) to visualize the shape of your output terms.
            // Debug.Log(RenderFuzzySetAscii(set));
            return set;
        }

        private FuzzySet<FzOutputWheel> GetWheelSet()
        {
            FuzzySet<FzOutputWheel> set = null;

            // TODO: Define this fuzzy output variable using GenerateDiscreteFuzzySet<T>().
            // Each enum label should have a representative crisp value for steering direction.

            // Example (pseudocode):
            // set = GenerateDiscreteFuzzySet<FzOutputWheel>( ... );
            // replace the following:

            // set = new FuzzySet<FzOutputWheel>();
            set = GenerateDiscreteFuzzySet<FzOutputWheel>(-1f, 0f, 1f);
            // You may test with: Debug.Log(RenderFuzzySetAscii(set));

            return set;
        }


        private FuzzyRuleSet<FzOutputThrottle> GetThrottleRuleSet(FuzzySet<FzOutputThrottle> throttle)
        {

            FuzzyRule<FzOutputThrottle>[] rules =
            {
                // TODO: Add some rules. Here is an example
                // (Note: these aren't necessarily good rules)
                If(FzInputSpeed.Slow).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Medium).Then(FzOutputThrottle.Coast),
                If(FzInputSpeed.Fast).Then(FzOutputThrottle.Brake),
                If(And(FzInputSpeed.Medium, Not(FzInputFutureCurvature.Straight))).Then(FzOutputThrottle.Brake),
                If(And(FzInputSpeed.Medium, FzInputFutureCurvature.Straight)).Then(FzOutputThrottle.Accelerate),
                // If(And(Or(FzInputFutureCurvature.TowardsLeft, FzInputFutureCurvature.TowardsRight), Not(FzInputSpeed.Slow))).Then(FzOutputThrottle.Brake),
                // More example syntax
                //If(And(FzInputSpeed.Fast, Not(FzFoo.Bar)).Then(FzOutputThrottle.Accelerate),
            };

            return new FuzzyRuleSet<FzOutputThrottle>(throttle, rules);
        }

        private FuzzyRuleSet<FzOutputWheel> GetWheelRuleSet(FuzzySet<FzOutputWheel> wheel)
        {

            FuzzyRule<FzOutputWheel>[] rules =
            {
                // TODO: Add some rules.
                If(FzInputFutureCurvature.TowardsLeft).Then(FzOutputWheel.TurnLeft),
                If(FzInputFutureCurvature.Straight).Then(FzOutputWheel.GoStraight),
                // If(FzInputLanePosition.Center).Then(FzOutputWheel.GoStraight),
                If(FzInputFutureCurvature.TowardsRight).Then(FzOutputWheel.TurnRight),
                // If(And(FzInputLanePosition.Right, FzInputFutureCurvature.TowardsRight)).Then(FzOutputWheel.TurnLeft),
                // If(And(FzInputLanePosition.Left, FzInputFutureCurvature.TowardsLeft)).Then(FzOutputWheel.TurnRight),
                // If(FzInputLanePosition.Left).Then(FzOutputWheel.TurnRight),
                // If(And(FzInputLanePosition.Left, FzInputFutureCurvature.TowardsLeft)).Then(FzOutputWheel.GoStraight),
                // If(And(FzInputLanePosition.Right, FzInputFutureCurvature.TowardsRight)).Then(FzOutputWheel.GoStraight),
            };

            return new FuzzyRuleSet<FzOutputWheel>(wheel, rules);
        }


        protected override void Awake()
        {
            base.Awake();

            StudentName = "Maganpreet Singh";

            // DO NOT INITIALIZE FUZZY STUFF HERE!!! Use Start() instead.
        }

        protected override void Start()
        {
            base.Start();

            // TODO: You can initialize a bunch of Fuzzy stuff here like more fuzzy inputs
            fzSpeedSet = this.GetSpeedSet();
            fzLanePositionSet = this.GetLanePositionSet();
            fzFutureCurvatureSet = this.GetCurvatureSet();

            fzThrottleSet = this.GetThrottleSet();
            fzThrottleRuleSet = this.GetThrottleRuleSet(fzThrottleSet);

            fzWheelSet = this.GetWheelSet();
            fzWheelRuleSet = this.GetWheelRuleSet(fzWheelSet);
        }

        System.Text.StringBuilder strBldr = new System.Text.StringBuilder();

        override protected void Update()
        {

            // TODO Do all your input fuzzification here and then
            // pass your fuzzy rule sets to ApplyFuzzyRules()
            
            // Remove the following hardcode calls once you get your fuzzy rules working.
            // You can leave one hardcoded while you work on the other.
            // Both steering and throttle must be implemented with variable
            // control and not fixed/hardcoded!

            // HardCodeSteering(0f);
            // HardCodeThrottle(0.5f);
            var dist = Racetrack.DistanceTravelled;
            var time = Time.time;

            // Debug.Log($"Distance: {dist}, Time: {time}, Speed: {Speed_kph}");

            // Simple example of fuzzification of vehicle state
            // The Speed is fuzzified and stored in fzInputValueSet
            fzSpeedSet.Evaluate(Speed_kph, fzInputValueSet);

            var forward = Racetrack.ClosestPointDirectionOnPath.normalized;
            // Debug.Log($"Forward: {forward}");
            forward.y = 0f;
            var left = new Vector3(-forward.z, 0f, forward.x);
            var centerPos = Racetrack.ClosestPointOnPath;
            var carPos = this.transform.position;

            // var forward = new Vector3(centerPos.x, 0f, centerPos.y).normalized;
            // var left = new Vector3(-forward.z, 0f, forward.x);
            var carPosFromCenter = carPos - centerPos;
            // need to recheck this but i think dot will negative if on right otherwise positive
            var laneOffset = Vector3.Dot(carPosFromCenter, left);

            // print($"Lane Offset: {laneOffset}, Halfwidth: {Racetrack.HalfRoadWidth}");
            fzLanePositionSet.Evaluate(laneOffset, fzInputValueSet);
            
            //Kunhao TA Suggestion
            var lookAheadDistance = Mathf.Clamp(Speed * 0.5f, 10, 40);
            // Debug.Log($"Lookahead Distance: {lookAheadDistance}");
            var roadAheadPoint = Racetrack.GetPointAhead(lookAheadDistance);
            var dirToPoint = roadAheadPoint - this.transform.position;
            dirToPoint.y = 0f;
            var curveAngle = Vector3.SignedAngle(this.transform.forward, dirToPoint.normalized, Vector3.up);
            // Debug.Log($"Curvature: {curveAngle}");
            fzFutureCurvatureSet.Evaluate(curveAngle, fzInputValueSet);
            // var futureCarPos = Racetrack.GetDirectionAhead(10f);
            // var nearestCenterToPointAhead = Racetrack.GetClosestPointDirectionOnPath(futureCarPos).normalized;

            // Debug.Log($"The future nearest point {nearestCenterToPointAhead}");
            
            // var futureCarPosFromCenter = futureCarPos - nearestCenterToPointAhead;

            // var curvature = Vector3.SignedAngle(nearestCenterToPointAhead, futureCarPosFromCenter, Vector3.up);

            // fzCurvatureSet.Evaluate(Curvature, fzInputValueSet);

            // ApplyFuzzyRules evaluates your rules and assigns Thottle and Steering accordingly
            // Also, some intermediate values are passed back for debugging purposes
            // Defuzzification output values as defined below are automatically assigned by ApplyFuzzyRules.
            // Throttle: [-1f, 1f] -1 is full brake, 0 is neutral, 1 is full throttle
            // Steering: [-1f, 1f] -1 if full left, 0 is neutral, 1 is full right
            // Note that you MUST use ApplyFuzzyRules(). You cannot direclty assign Throttle and Steering.

            ApplyFuzzyRules<FzOutputThrottle, FzOutputWheel>(
                fzThrottleRuleSet,
                fzWheelRuleSet,
                fzInputValueSet,
                // access to intermediate state for debugging
                out var throttleRuleOutput,
                out var wheelRuleOutput,
                ref mergedThrottle,
                ref mergedWheel
                );


            // Use vizText for debugging output
            // You might also use Debug.DrawLine() to draw vectors on Scene view
            // When you are done implementing, you can comment out this entire if statement.
            if (vizText != null)
            {
                strBldr.Clear();

                strBldr.AppendLine($"Demo Output");
                strBldr.AppendLine($"Comment out before submission");

                // You will probably want to selectively enable/disable printing
                // of certain fuzzy states or rules as you progress.

                DiagnosticPrintFuzzyValueSet<FzInputSpeed>(fzInputValueSet, strBldr);

                DiagnosticPrintRuleSet<FzOutputThrottle>(fzThrottleRuleSet, throttleRuleOutput, strBldr);
                DiagnosticPrintRuleSet<FzOutputWheel>(fzWheelRuleSet, wheelRuleOutput, strBldr);

                vizText.text = strBldr.ToString();
            }

            // Keep the base Update call at the end, after all your FuzzyVehicle code so that
            // control inputs can be processed properly (e.g. Throttle, Steering). base.Update() must be called or
            // autograder will fail.
            base.Update();
        }

    }
}
