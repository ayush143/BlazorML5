﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ML5
{
    public class NeuralNetwork
    {
        public IJSRuntime Runtime { get; set; }
        public string Hash { get; private set; }
        public Task<Layer[]> Layers
        {
            get
            {
                return GetLayers();
            }
        }
        public DotNetObjectReference<NeuralNetwork> DotNet { get; private set; }

        public NeuralNetwork(IJSRuntime jSRuntime,int inputs,int outputs)
        {
            Runtime = jSRuntime;
            Hash = Helper.UIDGenerator();
            Init(inputs, outputs);

        }
        private async Task<Layer[]> GetLayers()
        {
            int layerCount = await GetLayersInfo();
            List<Layer> layers = new List<Layer>();
            for (int i = 0; i < layerCount; i++)
            {
                layers.Add(new Layer(Runtime, Hash, i));
            }
            return layers.ToArray();
        }
        private async Task<int> GetLayersInfo()
        {
            return await Runtime.InvokeAsync<int>("getLayersInfoML5", Hash);
        }
        public NeuralNetwork(IJSRuntime jSRuntime, NeuralNetworkOptions options)
        {
            Runtime = jSRuntime;
            Hash = Helper.UIDGenerator();
            InitConfig(options);

        }
        public NeuralNetwork(IJSRuntime jSRuntime, NeuralNetworkOptions options,bool notifyModelLoad)
        {
            Runtime = jSRuntime;
            Hash = Helper.UIDGenerator();
            InitConfig(options,notifyModelLoad);
        }
        private async void Init(int inputs,int outputs)
        {
            await Runtime.InvokeVoidAsync("createNNML5", Hash,inputs,outputs);
        }
        private async void InitConfig(NeuralNetworkOptions options, bool isCallBack=false)
        {
            DotNet = DotNetObjectReference.Create(this);
            await Runtime.InvokeVoidAsync("createNNConfigML5", Hash, options,isCallBack,DotNet);
        }
        ~NeuralNetwork()
        {
            Destroy();
        }
        private async void Destroy()
        {
            await Runtime.InvokeVoidAsync("destroyNNML5", Hash);
        }
        public async void AddData(object xs,object ys)
        {
            await Runtime.InvokeVoidAsync("addDataML5",Hash, xs, ys);
        }
        public async void NormalizeData()
        {
            await Runtime.InvokeVoidAsync("normalizeDataML5", Hash);
        }
        /// <summary>
        /// Start training model
        /// </summary>
        /// <param name="trainingOptions"></param>
        /// <param name="subscribeCallBack">enable callbacks like whileTraining,doneTrainig</param>
        public async void Train(TrainingOptions trainingOptions=null,bool subscribeCallBack=true)
        {
            await Runtime.InvokeVoidAsync("trainML5", Hash, DotNet,subscribeCallBack,trainingOptions);
        }
        public async void Predict(object inputs)
        {
            await Runtime.InvokeVoidAsync("predictML5", Hash, DotNet, inputs);
        }
        public async void Classify(object inputs)
        {
            await Runtime.InvokeVoidAsync("classifyML5", Hash, DotNet, inputs);
        }

        //NeuralNet CallBack Model Load,While Train,Done Training
        [JSInvokable("NNCBML")]
        public async Task __ModelLoaded__()
        {
            OnModelLoaded?.Invoke();

        }
        [JSInvokable("NNCBWT")]
        public async Task __WhileTraining__(int epoch,double loss)
        {

            WhileTraining?.Invoke(epoch, loss);

        }
        [JSInvokable("NNCBDT")]
        public async Task __DoneTraining__()
        {
            OnTrainingComplete?.Invoke();
        }

        [JSInvokable("NNCBPD")]
        public async Task __Predict__(string error,Result[] result)
        {
            OnPredict?.Invoke(error,result);
        }
        [JSInvokable("NNCBCF")]
        public async Task __Classify__(string error, CResult[] result)
        {
            OnClassification?.Invoke(error, result);
        }

        public delegate void ModelLoadedHandler();
        public event ModelLoadedHandler OnModelLoaded;
        public delegate void DoneTrainingHandler();
        public event DoneTrainingHandler OnTrainingComplete;
        public delegate void WhileTrainingHandler(int epoch,double loss);
        public event WhileTrainingHandler WhileTraining;
        public delegate void OnPredictHandler(string error, Result[] result);
        public event OnPredictHandler OnPredict;
        public delegate void OnClassifyHandler(string error, CResult[] result);
        public event OnClassifyHandler OnClassification;
    }
}