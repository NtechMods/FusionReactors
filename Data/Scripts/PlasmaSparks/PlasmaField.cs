using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace PlasmaField
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "LBFusionReactorT1", "SBFusionReactorT1",
        "LBFusionReactorT2", "SBFusionReactorT2", "LBFusionReactorT3", "SBFusionReactorT3", "LBFusionReactorT4",
        "SBFusionReactorT4", "LBFusionReactorT5", "SBFusionReactorT5")]
    public class PlasmaFieldGameLogic : MyGameLogicComponent
    {
        private const float RotationRate = (float)Math.PI * 2;
        private const float SmallGridEffectScale = 0.2f;
        private MyReactor _reactor;
        private MyEntitySubpart _plasmaSubpart;
        private MyParticleEffect _effect;

        public override void OnAddedToContainer()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _reactor = (MyReactor)Entity;
            _reactor.IsWorkingChanged += Reactor_IsWorkingChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _reactor.IsWorkingChanged -= Reactor_IsWorkingChanged;
            StopEffect();
            _plasmaSubpart = null;
            _reactor = null;
        }

        private void Reactor_IsWorkingChanged(IMyCubeBlock block)
        {
            UpdateEffectState();
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated || _reactor == null)
                return;

            UpdateEffectState();
            if (!_reactor.IsWorking || _plasmaSubpart == null)
                return;

            float rotation = RotationRate * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            Matrix localMatrix = _plasmaSubpart.PositionComp.LocalMatrixRef;
            localMatrix *= Matrix.CreateRotationY(rotation);
            _plasmaSubpart.PositionComp.SetLocalMatrix(ref localMatrix);
            _plasmaSubpart.SetEmissiveParts("PlasmaEmissive", Color.Teal, 1f);

            if (_effect != null)
            {
                _effect.WorldMatrix = GetEffectWorldMatrix();
                _effect.Velocity = _reactor.CubeGrid.Physics == null
                    ? Vector3.Zero
                    : _reactor.CubeGrid.Physics.GetVelocityAtPoint(_effect.WorldMatrix.Translation);
            }
        }

        private void UpdateEffectState()
        {
            if (_reactor == null || _reactor.Model == null)
                return;

            if (_plasmaSubpart == null || _plasmaSubpart.Closed)
                _plasmaSubpart = _reactor.GetSubpart("PlasmaParticle");

            if (!_reactor.IsWorking || _plasmaSubpart == null)
            {
                if (_plasmaSubpart != null)
                    _plasmaSubpart.SetEmissiveParts("PlasmaEmissive", Color.Black, 0f);
                StopEffect();
                return;
            }

            if (_effect == null)
            {
                MyParticlesManager.TryCreateParticleEffect("PlasmaFieldEffect", out _effect);
            }

            if (_effect != null)
                _effect.WorldMatrix = GetEffectWorldMatrix();
        }

        private MatrixD GetEffectWorldMatrix()
        {
            MatrixD matrix = _plasmaSubpart.WorldMatrix;
            float scale = _reactor.BlockDefinition.CubeSize == MyCubeSize.Small
                ? SmallGridEffectScale
                : 1f;
            matrix.Right *= scale;
            matrix.Up *= scale;
            matrix.Backward *= scale;
            return matrix;
        }

        private void StopEffect()
        {
            if (_effect == null)
                return;

            _effect.Stop();
            MyParticlesManager.RemoveParticleEffect(_effect);
            _effect = null;
        }
    }
}
